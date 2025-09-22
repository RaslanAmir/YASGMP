import re, pathlib

root = pathlib.Path('.')
sql_path = root / 'YASGMP.sql'
tables_path = root / 'tables_without_models.txt'
output_dir = root / 'Models' / 'Generated'
output_dir.mkdir(parents=True, exist_ok=True)

sql_text = sql_path.read_text(encoding='utf-8', errors='ignore')

def normalize_type(sql_type: str):
    t = sql_type.lower()
    precision = None
    max_length = None
    clr_type = 'string'
    if '(' in t:
        base, rest = t.split('(', 1)
        rest = rest.split(')', 1)[0]
        if ',' in rest:
            try:
                precision = tuple(int(x.strip()) for x in rest.split(','))
            except ValueError:
                precision = None
        else:
            try:
                max_length = int(rest.strip())
            except ValueError:
                max_length = None
        t = base
    if t.strip().startswith('unsigned'):
        t = t.strip().split(' ', 1)[1]
    if 'tinyint(1' in sql_type.lower():
        clr_type = 'bool'
    elif 'bigint' in t:
        clr_type = 'long'
    elif t.startswith('int'):
        clr_type = 'int'
    elif t.startswith('smallint'):
        clr_type = 'short'
    elif t.startswith(('decimal', 'numeric')):
        clr_type = 'decimal'
    elif t.startswith('double'):
        clr_type = 'double'
    elif t.startswith('float'):
        clr_type = 'double'
    elif t.startswith(('datetime', 'timestamp', 'date')):
        clr_type = 'DateTime'
    elif t.startswith('time'):
        clr_type = 'TimeSpan'
    elif 'blob' in t or 'binary' in t:
        clr_type = 'byte[]'
    else:
        clr_type = 'string'
    return clr_type, max_length, precision

def is_nullable(definition: str):
    return 'not null' not in definition.lower()

create_pattern = re.compile(r'CREATE TABLE `(.*?)` \((.*?)\)\s*ENGINE', re.S)
column_pattern = re.compile(r'`(?P<name>\w+)`\s+(?P<type>[^,\n]+?)(?:,|$)', re.I)
primary_pattern = re.compile(r'PRIMARY KEY \((.*?)\)')

create_map = {table: body for table, body in create_pattern.findall(sql_text)}

def pascal_case(name: str) -> str:
    parts = re.split(r'[^A-Za-z0-9]+', name)
    parts = [p for p in parts if p]
    if not parts:
        return 'TableRecord'
    candidate = ''.join(p.capitalize() for p in parts)
    if candidate and candidate[0].isdigit():
        candidate = '_' + candidate
    return candidate

missing_tables = [line.strip() for line in tables_path.read_text(encoding='utf-8').splitlines() if line.strip()]

for table in missing_tables:
    body = create_map.get(table)
    if not body:
        print(f"Warning: table {table} not found in SQL dump")
        continue
    columns = []
    primary_cols = []
    for raw_line in body.split('\n'):
        line_strip = raw_line.strip()
        pk_match = primary_pattern.search(line_strip)
        if pk_match:
            pk_cols = [col.strip('` ') for col in pk_match.group(1).split(',')]
            primary_cols.extend(pk_cols)
        col_match = column_pattern.match(line_strip)
        if col_match:
            columns.append((col_match.group('name'), col_match.group('type'), line_strip))

    class_name = pascal_case(table)
    file_path = output_dir / f"{class_name}.cs"

    usings = {
        'System',
        'System.ComponentModel.DataAnnotations',
        'System.ComponentModel.DataAnnotations.Schema'
    }

    if any(col[1].lower().startswith(('decimal', 'numeric')) for col in columns):
        usings.add('Microsoft.EntityFrameworkCore')

    class_lines = []
    class_lines.append("    /// <summary>")
    class_lines.append(f"    /// Hyper-robust entity mapping for the `{table}` table, engineered for extreme enterprise/GxP analytics.")
    class_lines.append(f"    /// <para>Generated {class_name} ensures every column is surfaced for audits, AI, and compliance reporting.</para>")
    class_lines.append("    /// </summary>")
    class_lines.append(f"    [Table(\"{table}\")]")
    class_lines.append(f"    public class {class_name}")
    class_lines.append("    {")

    for col_name, type_def, raw_line in columns:
        clr_type, max_len, precision = normalize_type(type_def)
        nullable = is_nullable(raw_line)
        is_pk = col_name in primary_cols and len(primary_cols) == 1

        attributes = [f"[Column(\"{col_name}\")]" ]
        if is_pk:
            attributes.append('[Key]')
        if clr_type == 'string' and max_len:
            attributes.append(f"[MaxLength({max_len})]")
        if clr_type == 'decimal' and precision:
            try:
                p, s = precision
                attributes.append(f"[Precision({p}, {s})]")
            except Exception:
                pass

        property_type = clr_type
        default_suffix = ''
        if clr_type == 'string':
            if nullable:
                property_type = 'string?'
            else:
                default_suffix = ' = string.Empty;'
        elif clr_type == 'byte[]':
            if not nullable:
                default_suffix = ' = Array.Empty<byte>();'
        elif clr_type in {'int', 'long', 'short', 'double', 'decimal', 'DateTime', 'TimeSpan', 'bool'}:
            if nullable:
                property_type += '?'
        else:
            if nullable:
                property_type += '?'

        friendly_name = ' '.join(word.capitalize() for word in col_name.split('_')) or 'Value'
        class_lines.append("        /// <summary>")
        class_lines.append(f"        /// Column `{col_name}` ({type_def.strip()}) providing {friendly_name} fidelity.")
        class_lines.append("        /// </summary>")
        for attr in attributes:
            class_lines.append(f"        {attr}")
        property_name = pascal_case(col_name)
        class_lines.append(f"        public {property_type} {property_name} {{ get; set; }}{default_suffix}")
        class_lines.append("")

    class_lines.append("    }")

    file_content = '\n'.join(f"using {u};" for u in sorted(usings)) + '\n\nnamespace YasGMP.Models.Generated\n{\n' + '\n'.join(class_lines) + '\n}\n'

    file_path.write_text(file_content, encoding='utf-8')
    print(f"Generated model for {table} -> {file_path}")
