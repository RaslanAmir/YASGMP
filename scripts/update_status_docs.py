#!/usr/bin/env python3
"""Utility script to keep project status artifacts in sync.

This script normalizes ``docs/tasks.yaml`` and uses its metadata to
regenerate ``docs/STATUS.md`` and ``docs/EXECUTION_LOG.md``.  The command
is idempotent and supports a ``--check`` mode (for CI) as well as a
``--append-log`` workflow for adding new execution sessions.
"""
from __future__ import annotations

import argparse
import sys
from datetime import date, datetime
from pathlib import Path
from typing import Dict, Iterable, List, Tuple

import yaml


class IndentDumper(yaml.SafeDumper):
    def increase_indent(self, flow=False, indentless=False):
        return super().increase_indent(flow, False)


class QuotedString(str):
    """Marker type forcing PyYAML to single-quote string scalars."""


def represent_quoted_str(dumper: yaml.SafeDumper, data: QuotedString):
    return dumper.represent_scalar("tag:yaml.org,2002:str", str(data), style="'")


yaml.add_representer(QuotedString, represent_quoted_str, Dumper=IndentDumper)

ROOT = Path(__file__).resolve().parents[1]
TASKS_PATH = ROOT / "docs" / "tasks.yaml"
STATUS_PATH = ROOT / "docs" / "STATUS.md"
EXECUTION_LOG_PATH = ROOT / "docs" / "EXECUTION_LOG.md"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--check",
        action="store_true",
        help="Do not write files; exit with 1 if changes would be made.",
    )
    parser.add_argument(
        "--append-log",
        action="store_true",
        help="Append a new execution log entry using the --log-* arguments.",
    )
    parser.add_argument("--log-date", help="Date for the new session (YYYY-MM-DD).")
    parser.add_argument("--log-author", help="Author of the execution session.")
    parser.add_argument("--log-summary", help="Short summary for the session.")
    parser.add_argument(
        "--log-entry",
        action="append",
        help=(
            "Add a row to the session in the format 'HH:MM|Change|Notes'. "
            "Use multiple --log-entry options for multiple rows."
        ),
    )
    return parser.parse_args()


def load_tasks() -> Dict:
    if not TASKS_PATH.exists():
        return {}
    with TASKS_PATH.open("r", encoding="utf-8") as handle:
        content = handle.read()
    data = yaml.safe_load(content) if content.strip() else {}
    return data or {}


def normalize_metadata(metadata: Dict) -> Dict:
    ordered: Dict = {}
    for key in ("project_overview", "current_focus", "blockers", "next_steps"):
        if key in metadata:
            ordered[key] = metadata[key]
    for key, value in metadata.items():
        if key not in ordered:
            ordered[key] = value
    return ordered


def ensure_structure(data: Dict) -> Dict:
    data = dict(data)  # shallow copy
    metadata = data.get("metadata") or {}
    if not isinstance(metadata, dict):
        raise ValueError("metadata section in docs/tasks.yaml must be a mapping")
    tasks = data.get("tasks") or {}
    if not isinstance(tasks, dict):
        raise ValueError("tasks section in docs/tasks.yaml must be a mapping")
    execution_log = data.get("execution_log") or []
    if not isinstance(execution_log, list):
        raise ValueError("execution_log section in docs/tasks.yaml must be a list")

    # Ensure categories exist.
    ordered_tasks: Dict[str, List[Dict]] = {}
    for key in ("backlog", "in_progress", "completed"):
        value = tasks.get(key) or []
        if not isinstance(value, list):
            raise ValueError(f"tasks.{key} must be a list")
        ordered_tasks[key] = value
    for key, value in tasks.items():
        if key not in ordered_tasks:
            if not isinstance(value, list):
                raise ValueError(f"tasks.{key} must be a list")
            ordered_tasks[key] = value

    ordered: Dict[str, object] = {}
    ordered["metadata"] = normalize_metadata(metadata)
    ordered["tasks"] = ordered_tasks
    ordered["execution_log"] = execution_log
    return ordered


def append_log_entry(data: Dict, args: argparse.Namespace) -> None:
    if args.check:
        raise SystemExit("--append-log cannot be combined with --check")
    required = {"log_date": args.log_date, "log_author": args.log_author, "log_summary": args.log_summary}
    missing = [flag.replace("_", "-") for flag, value in required.items() if not value]
    if missing:
        raise SystemExit(
            "Missing required arguments for --append-log: " + ", ".join(f"--{name}" for name in missing)
        )
    if not args.log_entry:
        raise SystemExit("At least one --log-entry is required when using --append-log")

    try:
        log_date = datetime.strptime(args.log_date, "%Y-%m-%d").date()
    except ValueError as exc:
        raise SystemExit(f"Invalid --log-date value: {args.log_date!r}") from exc

    rows = []
    for raw in args.log_entry:
        parts = [segment.strip() for segment in raw.split("|")]
        if len(parts) != 3 or not parts[0]:
            raise SystemExit(
                "Each --log-entry must contain exactly two pipe characters and be in the format 'HH:MM|Change|Notes'."
            )
        timestamp, change, notes = parts
        try:
            datetime.strptime(timestamp, "%H:%M")
        except ValueError as exc:
            raise SystemExit(f"Invalid timestamp in --log-entry: {timestamp!r}") from exc
        rows.append({"timestamp": timestamp, "change": change, "notes": notes})

    new_entry = {
        "date": log_date.isoformat(),
        "author": args.log_author.strip(),
        "summary": args.log_summary.strip(),
        "entries": rows,
    }

    data.setdefault("execution_log", []).append(new_entry)


def sort_tasks(tasks: Dict[str, List[Dict]]) -> Dict[str, List[Dict]]:
    def task_sort_key(item: Dict) -> Tuple:
        due_value = item.get("due") or item.get("completed_on")
        if isinstance(due_value, (date, datetime)):
            due_value = due_value.date().isoformat() if isinstance(due_value, datetime) else due_value.isoformat()
        due = due_value or "9999-12-31"
        task_id = item.get("id") or item.get("title") or ""
        return (due, task_id)

    sorted_tasks = {}
    for category, entries in tasks.items():
        normalized_entries: List[Dict] = []
        for entry in entries:
            if not isinstance(entry, dict):
                raise ValueError(f"tasks.{category} must contain dictionaries")
            ordered: Dict[str, object] = {}
            for key in ("id", "title", "owner", "due", "completed_on", "notes"):
                if key in entry:
                    value = entry[key]
                    if key in {"due", "completed_on"}:
                        if isinstance(value, (date, datetime)):
                            value = value.date().isoformat() if isinstance(value, datetime) else value.isoformat()
                        if isinstance(value, str):
                            value = QuotedString(value)
                    ordered[key] = value
            # Preserve any additional keys with stable ordering.
            for key, value in entry.items():
                if key not in ordered:
                    ordered[key] = value
            normalized_entries.append(ordered)
        normalized_entries.sort(key=task_sort_key)
        sorted_tasks[category] = normalized_entries
    return sorted_tasks


def sort_execution_log(entries: List[Dict]) -> List[Dict]:
    normalized = []
    for entry in entries:
        if not isinstance(entry, dict):
            raise ValueError("execution_log must contain dictionaries")
        try:
            date = datetime.strptime(str(entry.get("date")), "%Y-%m-%d").date()
        except ValueError as exc:
            raise ValueError(f"Invalid date in execution_log entry: {entry!r}") from exc
        author = str(entry.get("author", "")).strip()
        summary = str(entry.get("summary", "")).strip()
        rows = entry.get("entries") or []
        if not isinstance(rows, list):
            raise ValueError("execution_log entries must contain an 'entries' list")
        normalized_rows = []
        for row in rows:
            if not isinstance(row, dict):
                raise ValueError("execution_log entries must contain dictionaries")
            timestamp = str(row.get("timestamp", "")).strip()
            try:
                datetime.strptime(timestamp, "%H:%M")
            except ValueError as exc:
                raise ValueError(f"Invalid timestamp in execution log rows: {row!r}") from exc
            change = str(row.get("change", "")).strip()
            notes = str(row.get("notes", "")).strip()
            normalized_rows.append({"timestamp": QuotedString(timestamp), "change": change, "notes": notes})
        normalized_rows.sort(key=lambda row: row["timestamp"])
        normalized_entry = {
            "date": date.isoformat(),
            "author": author,
            "summary": summary,
            "entries": normalized_rows,
        }
        normalized.append((date, normalized_entry))
    normalized.sort(key=lambda item: item[0], reverse=True)
    return [entry for _, entry in normalized]


def serialize_yaml(data: Dict) -> str:
    return (
        yaml.dump(
            data,
            Dumper=IndentDumper,
            sort_keys=False,
            allow_unicode=True,
            default_flow_style=False,
        ).strip()
        + "\n"
    )


def render_status_md(metadata: Dict, tasks: Dict[str, List[Dict]]) -> str:
    lines: List[str] = []
    overview = metadata.get("project_overview", "").strip()
    if not overview:
        overview = "Project overview is not yet documented."
    lines.extend(["# Project Overview", "", overview, ""])

    lines.extend(["# Current Status", ""])
    current_focus: Iterable[str] = metadata.get("current_focus") or []
    if current_focus:
        for item in current_focus:
            lines.append(f"- {item}")
    else:
        in_progress = tasks.get("in_progress", [])
        if in_progress:
            for task in in_progress:
                owner = task.get("owner", "Unassigned")
                due = task.get("due") or "TBD"
                lines.append(f"- {task.get('id', 'N/A')}: {task.get('title', 'Untitled')} (owner: {owner}, due: {due})")
        else:
            lines.append("- No active work items recorded.")
    lines.append("")

    lines.extend(["# Blockers", ""])
    blockers = metadata.get("blockers") or []
    if blockers:
        for blocker in blockers:
            lines.append(f"- {blocker}")
    else:
        lines.append("No active blockers.")
    lines.append("")

    lines.extend(["# Next Steps", ""])
    next_steps = metadata.get("next_steps") or []
    if next_steps:
        for step in next_steps:
            lines.append(f"- {step}")
    else:
        backlog = tasks.get("backlog", [])
        if backlog:
            for task in backlog:
                lines.append(f"- {task.get('id', 'N/A')}: {task.get('title', 'Untitled')}")
        else:
            lines.append("- No upcoming tasks queued.")
    lines.append("")
    return "\n".join(lines)


def escape_table_cell(value: str) -> str:
    return value.replace("|", r"\|")


def render_execution_log_md(entries: List[Dict]) -> str:
    front_matter: Dict[str, object] = {}
    front_matter["sessions_tracked"] = len(entries)
    if entries:
        latest = entries[0]
        front_matter["latest_date"] = latest["date"]
        front_matter["latest_author"] = latest["author"]
        front_matter["latest_summary"] = latest["summary"]
    fm_body = yaml.dump(
        front_matter,
        Dumper=IndentDumper,
        sort_keys=False,
        allow_unicode=True,
        default_flow_style=False,
    ).strip()
    lines = ["---", fm_body, "---", "", "# Execution Log", ""]

    for entry in entries:
        heading = f"## {entry['date']} — {entry['summary']} ({entry['author']})".strip()
        lines.extend([heading, "", "| Timestamp | Change | Notes |", "|-----------|--------|-------|"])
        for row in entry["entries"]:
            lines.append(
                "| {timestamp} | {change} | {notes} |".format(
                    timestamp=escape_table_cell(row["timestamp"]),
                    change=escape_table_cell(row["change"]),
                    notes=escape_table_cell(row["notes"]),
                )
            )
        lines.append("")
    return "\n".join(lines).rstrip() + "\n"


def write_if_changed(path: Path, content: str, check: bool, pending: List[Path]) -> None:
    current = path.read_text(encoding="utf-8") if path.exists() else ""
    if current == content:
        return
    if check:
        pending.append(path)
    else:
        path.write_text(content, encoding="utf-8")


def main() -> None:
    args = parse_args()
    data = ensure_structure(load_tasks())

    if args.append_log:
        append_log_entry(data, args)

    data["tasks"] = sort_tasks(data["tasks"])
    data["execution_log"] = sort_execution_log(data["execution_log"])

    pending: List[Path] = []

    tasks_yaml = serialize_yaml(data)
    write_if_changed(TASKS_PATH, tasks_yaml, args.check, pending)

    status_md = render_status_md(data["metadata"], data["tasks"])
    write_if_changed(STATUS_PATH, status_md, args.check, pending)

    execution_log_md = render_execution_log_md(data["execution_log"])
    write_if_changed(EXECUTION_LOG_PATH, execution_log_md, args.check, pending)

    if args.check:
        if pending:
            rel_paths = ", ".join(str(path.relative_to(ROOT)) for path in pending)
            print(f"Status documentation is out of date: {rel_paths}", file=sys.stderr)
            sys.exit(1)
        return

    if args.append_log:
        last = data["execution_log"][0]
        print(
            f"Appended execution session for {last['date']} by {last['author']}.",
            file=sys.stdout,
        )


if __name__ == "__main__":
    try:
        main()
    except ValueError as exc:
        print(f"error: {exc}", file=sys.stderr)
        sys.exit(1)
