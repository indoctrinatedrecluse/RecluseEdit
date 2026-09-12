using System.IO;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace RecluseEdit.Extensions.Database.Syntaxes;

/// <summary>
/// Provides an AvalonEdit XML Syntax Highlighting Definition (XSHD) for SQL,
/// supporting ANSI SQL, SQLite, PostgreSQL, MySQL, and T-SQL dialects in Dark+ styling.
/// </summary>
public static class SqlSyntaxDefinition
{
    private const string SqlXshd = """
        <?xml version="1.0"?>
        <SyntaxDefinition name="SQL" extensions=".sql" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
            <Color name="Digits" foreground="#B5CEA8" />
            <Color name="String" foreground="#CE9178" />
            <Color name="Keywords" foreground="#569CD6" fontWeight="bold" />
            <Color name="Types" foreground="#4EC9B0" />
            <Color name="Functions" foreground="#DCDCAA" />
            <Color name="Comment" foreground="#6A9955" />
            <Color name="Operators" foreground="#D4D4D4" />
            <Color name="Identifier" foreground="#9CDCFE" />

            <RuleSet ignoreCase="true">
                <!-- Single line comment: - - -->
                <Span color="Comment">
                    <Begin>--</Begin>
                </Span>

                <!-- Multi-line comment: /* ... */ -->
                <Span color="Comment" multiline="true">
                    <Begin>/\*</Begin>
                    <End>\*/</End>
                </Span>

                <!-- Strings: '...' -->
                <Span color="String" multiline="true">
                    <Begin>'</Begin>
                    <End>'</End>
                    <RuleSet>
                        <Span begin="''" end="" />
                    </RuleSet>
                </Span>

                <!-- Quoted identifiers: `...` (MySQL/SQLite) -->
                <Span color="Identifier">
                    <Begin>`</Begin>
                    <End>`</End>
                </Span>

                <!-- Bracketed identifiers: [...] (T-SQL/SQLite) -->
                <Span color="Identifier">
                    <Begin>\[</Begin>
                    <End>\]</End>
                </Span>

                <!-- Double-quoted identifiers: "..." (ANSI SQL/PostgreSQL) -->
                <Span color="Identifier">
                    <Begin>"</Begin>
                    <End>"</End>
                </Span>

                <!-- Numbers -->
                <Rule color="Digits">\b0[xX][0-9a-fA-F]+\b|\b\d+(\.[0-9]+)?\b</Rule>

                <!-- SQL Keywords -->
                <Keywords color="Keywords">
                    <Word>SELECT</Word>
                    <Word>FROM</Word>
                    <Word>WHERE</Word>
                    <Word>INSERT</Word>
                    <Word>INTO</Word>
                    <Word>VALUES</Word>
                    <Word>UPDATE</Word>
                    <Word>SET</Word>
                    <Word>DELETE</Word>
                    <Word>JOIN</Word>
                    <Word>INNER</Word>
                    <Word>LEFT</Word>
                    <Word>RIGHT</Word>
                    <Word>FULL</Word>
                    <Word>OUTER</Word>
                    <Word>CROSS</Word>
                    <Word>ON</Word>
                    <Word>GROUP</Word>
                    <Word>BY</Word>
                    <Word>ORDER</Word>
                    <Word>HAVING</Word>
                    <Word>LIMIT</Word>
                    <Word>OFFSET</Word>
                    <Word>UNION</Word>
                    <Word>ALL</Word>
                    <Word>DISTINCT</Word>
                    <Word>AS</Word>
                    <Word>CASE</Word>
                    <Word>WHEN</Word>
                    <Word>THEN</Word>
                    <Word>ELSE</Word>
                    <Word>END</Word>
                    <Word>CREATE</Word>
                    <Word>TABLE</Word>
                    <Word>VIEW</Word>
                    <Word>INDEX</Word>
                    <Word>ALTER</Word>
                    <Word>DROP</Word>
                    <Word>ADD</Word>
                    <Word>COLUMN</Word>
                    <Word>CONSTRAINT</Word>
                    <Word>PRIMARY</Word>
                    <Word>KEY</Word>
                    <Word>FOREIGN</Word>
                    <Word>REFERENCES</Word>
                    <Word>DEFAULT</Word>
                    <Word>CHECK</Word>
                    <Word>UNIQUE</Word>
                    <Word>NOT</Word>
                    <Word>NULL</Word>
                    <Word>AND</Word>
                    <Word>OR</Word>
                    <Word>IN</Word>
                    <Word>EXISTS</Word>
                    <Word>BETWEEN</Word>
                    <Word>LIKE</Word>
                    <Word>IS</Word>
                    <Word>BEGIN</Word>
                    <Word>TRANSACTION</Word>
                    <Word>COMMIT</Word>
                    <Word>ROLLBACK</Word>
                    <Word>CASCADE</Word>
                    <Word>RESTRICT</Word>
                    <Word>TRIGGER</Word>
                    <Word>PROCEDURE</Word>
                    <Word>FUNCTION</Word>
                    <Word>RETURNING</Word>
                    <Word>WITH</Word>
                    <Word>RECURSIVE</Word>
                    <Word>DESC</Word>
                    <Word>ASC</Word>
                    <Word>TRUE</Word>
                    <Word>FALSE</Word>
                </Keywords>

                <!-- Built-in Functions -->
                <Keywords color="Functions">
                    <Word>COUNT</Word>
                    <Word>SUM</Word>
                    <Word>AVG</Word>
                    <Word>MIN</Word>
                    <Word>MAX</Word>
                    <Word>COALESCE</Word>
                    <Word>NULLIF</Word>
                    <Word>CONCAT</Word>
                    <Word>SUBSTRING</Word>
                    <Word>LOWER</Word>
                    <Word>UPPER</Word>
                    <Word>LENGTH</Word>
                    <Word>TRIM</Word>
                    <Word>ROUND</Word>
                    <Word>FLOOR</Word>
                    <Word>CEIL</Word>
                    <Word>ABS</Word>
                    <Word>NOW</Word>
                    <Word>CURRENT_TIMESTAMP</Word>
                    <Word>CURRENT_DATE</Word>
                    <Word>CURRENT_TIME</Word>
                    <Word>DATE</Word>
                    <Word>CAST</Word>
                    <Word>CONVERT</Word>
                </Keywords>

                <!-- Data Types -->
                <Keywords color="Types">
                    <Word>INT</Word>
                    <Word>INTEGER</Word>
                    <Word>BIGINT</Word>
                    <Word>SMALLINT</Word>
                    <Word>TINYINT</Word>
                    <Word>NUMERIC</Word>
                    <Word>DECIMAL</Word>
                    <Word>FLOAT</Word>
                    <Word>DOUBLE</Word>
                    <Word>REAL</Word>
                    <Word>BOOLEAN</Word>
                    <Word>BOOL</Word>
                    <Word>VARCHAR</Word>
                    <Word>NVARCHAR</Word>
                    <Word>CHAR</Word>
                    <Word>TEXT</Word>
                    <Word>BLOB</Word>
                    <Word>CLOB</Word>
                    <Word>DATE</Word>
                    <Word>TIME</Word>
                    <Word>DATETIME</Word>
                    <Word>TIMESTAMP</Word>
                    <Word>TIMESTAMPTZ</Word>
                    <Word>JSON</Word>
                    <Word>JSONB</Word>
                    <Word>UUID</Word>
                    <Word>SERIAL</Word>
                    <Word>BIGSERIAL</Word>
                    <Word>AUTOINCREMENT</Word>
                </Keywords>
            </RuleSet>
        </SyntaxDefinition>
        """;

    public static IHighlightingDefinition CreateDefinition()
    {
        using var stringReader = new StringReader(SqlXshd);
        using var xmlReader = XmlReader.Create(stringReader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }
}

