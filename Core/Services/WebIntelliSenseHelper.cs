using ICSharpCode.AvalonEdit.CodeCompletion;

namespace RecluseEdit.Core.Services;

/// <summary>
/// Provides comprehensive completion data for HTML, CSS, JavaScript, and TypeScript IntelliSense.
/// </summary>
public static class WebIntelliSenseHelper
{
    private static readonly List<WebCompletionData> HtmlItems =
    [
        new("!DOCTYPE", "HTML5 Doctype declaration", 100),
        new("html", "Root HTML element", 90),
        new("head", "Document metadata container", 85),
        new("body", "Document body container", 85),
        new("meta", "Document metadata element", 80),
        new("title", "Document title", 80),
        new("link", "External resource link (e.g. stylesheet)", 80),
        new("style", "Embedded style element", 80),
        new("script", "Embedded or external script element", 80),
        new("div", "Generic block container", 95),
        new("span", "Generic inline container", 90),
        new("button", "Clickable button", 85),
        new("input", "Interactive form input", 85),
        new("form", "Interactive form container", 85),
        new("label", "Caption for a user interface item", 80),
        new("textarea", "Multi-line plain-text editing control", 80),
        new("select", "Drop-down selection list", 80),
        new("option", "Item in a select element", 75),
        new("a", "Hyperlink anchor", 90),
        new("img", "Image embed element", 85),
        new("p", "Paragraph element", 90),
        new("h1", "Top-level section heading", 85),
        new("h2", "Second-level section heading", 85),
        new("h3", "Third-level section heading", 80),
        new("h4", "Fourth-level section heading", 75),
        new("ul", "Unordered list container", 85),
        new("ol", "Ordered list container", 80),
        new("li", "List item", 85),
        new("table", "Table container", 80),
        new("thead", "Table header group", 75),
        new("tbody", "Table body group", 75),
        new("tr", "Table row", 80),
        new("th", "Table header cell", 80),
        new("td", "Table data cell", 80),
        new("nav", "Navigation links section", 80),
        new("header", "Header landmark container", 80),
        new("footer", "Footer landmark container", 80),
        new("main", "Main content of the document", 80),
        new("section", "Generic standalone section", 80),
        new("article", "Self-contained composition", 75),
        new("aside", "Portion of document indirectly related", 70),
        new("video", "Embedded video media player", 75),
        new("audio", "Embedded audio media player", 75),
        new("canvas", "Scriptable bitmap canvas", 75),
        new("svg", "Scalable Vector Graphics container", 75),
        // Attributes
        new("class", "HTML element CSS class name", 60),
        new("id", "Unique element identifier", 60),
        new("style", "Inline CSS styling", 60),
        new("src", "Address of the external resource", 60),
        new("href", "Target URL of the link", 60),
        new("type", "Type of the element/input", 60),
        new("name", "Name of the form control", 60),
        new("value", "Value of the form control", 60),
        new("placeholder", "Short hint for input field", 60),
        new("alt", "Alternative text for image", 60)
    ];

    private static readonly List<WebCompletionData> CssItems =
    [
        new("display", "Specifies the display behavior (e.g. flex, grid, block, none)", 95),
        new("flex-direction", "Direction of flexible items (row, column)", 90),
        new("justify-content", "Aligns items along main axis (center, space-between, flex-start)", 90),
        new("align-items", "Aligns items along cross axis (center, stretch, flex-start)", 90),
        new("gap", "Gutter size between grid and flex items", 85),
        new("position", "Positioning method (relative, absolute, fixed, sticky)", 90),
        new("top", "Top coordinate position", 80),
        new("right", "Right coordinate position", 80),
        new("bottom", "Bottom coordinate position", 80),
        new("left", "Left coordinate position", 80),
        new("z-index", "Stack order of an element", 80),
        new("margin", "Sets margin area on all four sides", 90),
        new("padding", "Sets padding area on all four sides", 90),
        new("width", "Sets width of an element", 90),
        new("height", "Sets height of an element", 90),
        new("max-width", "Sets maximum width of an element", 85),
        new("min-width", "Sets minimum width of an element", 80),
        new("max-height", "Sets maximum height of an element", 80),
        new("min-height", "Sets minimum height of an element", 80),
        new("color", "Sets foreground color of text", 90),
        new("background", "Sets background shorthand properties", 90),
        new("background-color", "Sets background color", 90),
        new("background-image", "Sets background image", 80),
        new("background-size", "Specifies size of background images", 80),
        new("border", "Sets border shorthand properties", 90),
        new("border-radius", "Rounds the corners of element's outer border edge", 90),
        new("font-family", "Specifies font family list", 85),
        new("font-size", "Sets size of the font", 85),
        new("font-weight", "Sets weight/boldness of font", 85),
        new("line-height", "Sets distance between lines of text", 80),
        new("text-align", "Sets horizontal alignment of inline content", 80),
        new("text-decoration", "Sets text decoration (underline, none)", 80),
        new("box-shadow", "Attaches one or more shadows to an element", 85),
        new("box-sizing", "Sets how total width/height is calculated (border-box)", 85),
        new("cursor", "Mouse cursor to show when pointing over element (pointer)", 80),
        new("opacity", "Transparency level of an element (0.0 to 1.0)", 80),
        new("overflow", "Specifies clipping behavior (hidden, auto, scroll)", 80),
        new("transition", "Shorthand for transition-property, duration, timing, delay", 85),
        new("transform", "Applies 2D or 3D transformation (translate, rotate, scale)", 85),
        // Common Values
        new("flex", "Display value for flexible box layout", 70),
        new("grid", "Display value for CSS grid layout", 70),
        new("block", "Display value for block box", 70),
        new("inline-block", "Display value for inline block box", 70),
        new("none", "Hides the element", 70),
        new("relative", "Position relative to normal position", 70),
        new("absolute", "Position relative to nearest positioned ancestor", 70),
        new("border-box", "Box model includes padding and border", 70),
        new("pointer", "Cursor pointer icon for clickable elements", 70)
    ];

    private static readonly List<WebCompletionData> JsItems =
    [
        new("console", "Console logging utility (log, warn, error)", 95),
        new("document", "Direct access to the DOM document", 95),
        new("window", "Global browser window object", 90),
        new("addEventListener", "Attaches an event handler function to an element", 90),
        new("removeEventListener", "Removes an event handler function", 80),
        new("getElementById", "Returns reference to the element by its ID", 90),
        new("querySelector", "Returns first element matching CSS selector", 90),
        new("querySelectorAll", "Returns all elements matching CSS selector", 85),
        new("createElement", "Creates specified HTML element node", 85),
        new("appendChild", "Adds a node to the end of list of children", 85),
        new("removeChild", "Removes child node from the DOM", 80),
        new("setAttribute", "Sets the value of an attribute on specified element", 80),
        new("getAttribute", "Returns value of specified attribute", 80),
        new("classList", "Manipulates element class list (add, remove, toggle)", 85),
        new("innerHTML", "Gets or sets HTML markup contained within element", 85),
        new("textContent", "Gets or sets text content of element node", 85),
        new("fetch", "Initiates HTTP network request returning a Promise", 90),
        new("Promise", "Object representing eventual completion of async operation", 85),
        new("JSON", "JavaScript Object Notation parser and serializer", 90),
        new("Math", "Standard math constants and functions", 85),
        new("setTimeout", "Schedules function to run after specified delay", 85),
        new("setInterval", "Repeatedly calls function with fixed time delay", 80),
        new("clearTimeout", "Cancels timeout previously established", 75),
        new("clearInterval", "Cancels repeating timed action", 75),
        new("Array", "Global Array constructor", 85),
        new("Object", "Global Object constructor", 85),
        new("String", "Global String constructor", 80),
        new("Number", "Global Number constructor", 80),
        new("Boolean", "Global Boolean constructor", 75),
        new("Date", "Date and time object constructor", 80),
        // ES Keywords
        new("const", "Declares a block-scoped, read-only constant", 95),
        new("let", "Declares a block-scoped local variable", 90),
        new("function", "Declares a function with specified parameters", 90),
        new("async", "Defines an asynchronous function", 90),
        new("await", "Pauses async function execution until Promise settles", 90),
        new("return", "Specifies the value to be returned by function", 90),
        new("import", "Imports bindings exported by another module", 85),
        new("export", "Exports functions, objects or primitives from a module", 85),
        new("if", "Conditional branching statement", 85),
        new("else", "Alternative conditional branching statement", 80),
        new("switch", "Evaluates expression matching against case clauses", 80),
        new("case", "Case clause within switch statement", 75),
        new("try", "Marks block of statements to try with catch handler", 80),
        new("catch", "Handles exception thrown within try block", 80),
        new("finally", "Executes statements after try and catch blocks", 75),
        new("throw", "Throws a user-defined exception", 75),
        new("new", "Creates instance of user-defined object type", 80),
        new("typeof", "Returns string indicating type of unevaluated operand", 80),
        new("instanceof", "Tests whether prototype property appears in chain", 75),
        // Array / Object methods
        new("map", "Creates new array populated with results of calling callback", 85),
        new("filter", "Creates shallow copy of array filtered by test callback", 85),
        new("forEach", "Executes provided callback once for each array element", 85),
        new("reduce", "Executes reducer function on each array element", 80),
        new("find", "Returns first array element that satisfies testing function", 80),
        new("includes", "Determines whether an array/string includes specified value", 80),
        new("length", "Length property of string or array", 85),
        new("push", "Appends new elements to end of an array", 80),
        new("pop", "Removes last element from an array and returns it", 75)
    ];

    public static IList<ICompletionData> GetCompletions(string languageId, string wordPrefix)
    {
        var source = languageId.ToLowerInvariant() switch
        {
            "html" or "xml" => HtmlItems,
            "css" => CssItems,
            "javascript" or "typescript" or "json" => JsItems,
            _ => HtmlItems
        };

        if (string.IsNullOrEmpty(wordPrefix))
        {
            return source.Cast<ICompletionData>().ToList();
        }

        return source
            .Where(item => item.Text.StartsWith(wordPrefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.Priority)
            .Cast<ICompletionData>()
            .ToList();
    }
}

