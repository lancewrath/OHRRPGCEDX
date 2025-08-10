using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OHRRPGCEDX.Scripting
{
    /// <summary>
    /// HamsterSpeak script interpreter for OHRRPGCE
    /// </summary>
    public class ScriptEngine
    {
        private Dictionary<string, ScriptFunction> builtinFunctions;
        private Dictionary<string, object> globalVariables;
        private Dictionary<string, ScriptFunctionDefinition> userFunctions;
        private Stack<ScriptContext> callStack;
        private bool isInitialized;

        public ScriptEngine()
        {
            builtinFunctions = new Dictionary<string, ScriptFunction>();
            globalVariables = new Dictionary<string, object>();
            userFunctions = new Dictionary<string, ScriptFunction>();
            callStack = new Stack<ScriptContext>();
        }

        /// <summary>
        /// Initialize the script engine
        /// </summary>
        public bool Initialize()
        {
            try
            {
                // Register built-in functions
                RegisterBuiltinFunctions();
                
                isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize script engine: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Register built-in HamsterSpeak functions
        /// </summary>
        private void RegisterBuiltinFunctions()
        {
            // Text and display functions
            RegisterFunction("show text box", ShowTextBox);
            RegisterFunction("hide text box", HideTextBox);
            RegisterFunction("wait for text box", WaitForTextBox);
            RegisterFunction("show string", ShowString);
            RegisterFunction("hide string", HideString);
            
            // Variable functions
            RegisterFunction("set variable", SetVariable);
            RegisterFunction("get variable", GetVariable);
            RegisterFunction("set hero stat", SetHeroStat);
            RegisterFunction("get hero stat", GetHeroStat);
            
            // Map and movement functions
            RegisterFunction("teleport to map", TeleportToMap);
            RegisterFunction("teleport to position", TeleportToPosition);
            RegisterFunction("move hero", MoveHero);
            RegisterFunction("set hero direction", SetHeroDirection);
            
            // Item and inventory functions
            RegisterFunction("give item", GiveItem);
            RegisterFunction("take item", TakeItem);
            RegisterFunction("check item", CheckItem);
            RegisterFunction("set item count", SetItemCount);
            
            // Battle functions
            RegisterFunction("start battle", StartBattle);
            RegisterFunction("end battle", EndBattle);
            RegisterFunction("set enemy stat", SetEnemyStat);
            RegisterFunction("change enemy sprite", ChangeEnemySprite);
            
            // Sound and music functions
            RegisterFunction("play music", PlayMusic);
            RegisterFunction("stop music", StopMusic);
            RegisterFunction("play sound", PlaySound);
            RegisterFunction("set volume", SetVolume);
            
            // Menu functions
            RegisterFunction("show menu", ShowMenu);
            RegisterFunction("hide menu", HideMenu);
            RegisterFunction("set menu option", SetMenuOption);
            
            // Conditional and control functions
            RegisterFunction("if", IfStatement);
            RegisterFunction("else", ElseStatement);
            RegisterFunction("end if", EndIfStatement);
            RegisterFunction("while", WhileLoop);
            RegisterFunction("end while", EndWhileLoop);
            RegisterFunction("break", BreakStatement);
            RegisterFunction("continue", ContinueStatement);
            
            // Math functions
            RegisterFunction("add", Add);
            RegisterFunction("subtract", Subtract);
            RegisterFunction("multiply", Multiply);
            RegisterFunction("divide", Divide);
            RegisterFunction("modulo", Modulo);
            RegisterFunction("random", Random);
            
            // String functions
            RegisterFunction("string length", StringLength);
            RegisterFunction("substring", Substring);
            RegisterFunction("string equals", StringEquals);
            RegisterFunction("concatenate", Concatenate);
        }

        /// <summary>
        /// Register a built-in function
        /// </summary>
        private void RegisterFunction(string name, ScriptFunction function)
        {
            builtinFunctions[name.ToLower()] = function;
        }

        /// <summary>
        /// Execute a HamsterSpeak script
        /// </summary>
        public object ExecuteScript(string script)
        {
            if (!isInitialized) return null;

            try
            {
                var tokens = TokenizeScript(script);
                var ast = ParseScript(tokens);
                return ExecuteAST(ast);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Script execution error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Execute a script file
        /// </summary>
        public object ExecuteScriptFile(string filePath)
        {
            try
            {
                var script = System.IO.File.ReadAllText(filePath);
                return ExecuteScript(script);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to execute script file {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Tokenize the script into tokens
        /// </summary>
        private List<ScriptToken> TokenizeScript(string script)
        {
            var tokens = new List<ScriptToken>();
            var lines = script.Split('\n');
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                    continue;
                
                var words = trimmedLine.Split(' ');
                foreach (var word in words)
                {
                    if (!string.IsNullOrEmpty(word))
                    {
                        tokens.Add(new ScriptToken { Value = word.Trim(), Type = DetermineTokenType(word) });
                    }
                }
            }
            
            return tokens;
        }

        /// <summary>
        /// Determine the type of a token
        /// </summary>
        private TokenType DetermineTokenType(string word)
        {
            if (int.TryParse(word, out _))
                return TokenType.Number;
            if (word.StartsWith("\"") && word.EndsWith("\""))
                return TokenType.String;
            if (word.StartsWith("$"))
                return TokenType.Variable;
            if (word.StartsWith("(") || word.StartsWith(")"))
                return TokenType.Parenthesis;
            if (word.StartsWith("[") || word.StartsWith("]"))
                return TokenType.Bracket;
            if (word.StartsWith(","))
                return TokenType.Comma;
            
            return TokenType.Identifier;
        }

        /// <summary>
        /// Parse tokens into an Abstract Syntax Tree
        /// </summary>
        private ScriptNode ParseScript(List<ScriptToken> tokens)
        {
            // Simple parser - in a real implementation, you'd have a more sophisticated parser
            var rootNode = new ScriptNode { Type = NodeType.Root };
            var currentNode = rootNode;
            
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                
                switch (token.Type)
                {
                    case TokenType.Identifier:
                        var functionNode = new ScriptNode
                        {
                            Type = NodeType.FunctionCall,
                            Value = token.Value
                        };
                        currentNode.Children.Add(functionNode);
                        currentNode = functionNode;
                        break;
                        
                    case TokenType.Number:
                    case TokenType.String:
                    case TokenType.Variable:
                        var valueNode = new ScriptNode
                        {
                            Type = NodeType.Value,
                            Value = token.Value
                        };
                        currentNode.Children.Add(valueNode);
                        break;
                        
                    case TokenType.Parenthesis:
                    case TokenType.Bracket:
                        // Handle grouping
                        break;
                }
            }
            
            return rootNode;
        }

        /// <summary>
        /// Execute the Abstract Syntax Tree
        /// </summary>
        private object ExecuteAST(ScriptNode node)
        {
            switch (node.Type)
            {
                case NodeType.Root:
                    object result = null;
                    foreach (var child in node.Children)
                    {
                        result = ExecuteAST(child);
                    }
                    return result;
                    
                case NodeType.FunctionCall:
                    return ExecuteFunction(node);
                    
                case NodeType.Value:
                    return ParseValue(node.Value);
                    
                default:
                    return null;
            }
        }

        /// <summary>
        /// Execute a function call
        /// </summary>
        private object ExecuteFunction(ScriptNode node)
        {
            var functionName = node.Value.ToString().ToLower();
            
            // Collect arguments
            var arguments = new List<object>();
            foreach (var child in node.Children)
            {
                if (child.Type == NodeType.Value)
                {
                    arguments.Add(ParseValue(child.Value));
                }
            }
            
            // Execute built-in function
            if (builtinFunctions.ContainsKey(functionName))
            {
                return builtinFunctions[functionName](arguments.ToArray());
            }
            
            // Execute user-defined function
            if (userFunctions.ContainsKey(functionName))
            {
                return ExecuteUserFunction(functionName, arguments.ToArray());
            }
            
            Console.WriteLine($"Unknown function: {functionName}");
            return null;
        }

        /// <summary>
        /// Execute a user-defined function
        /// </summary>
        private object ExecuteUserFunction(string functionName, object[] arguments)
        {
            var function = userFunctions[functionName];
            var context = new ScriptContext
            {
                FunctionName = functionName,
                Arguments = arguments,
                LocalVariables = new Dictionary<string, object>()
            };
            
            callStack.Push(context);
            var result = ExecuteAST(function.Body);
            callStack.Pop();
            
            return result;
        }

        /// <summary>
        /// Parse a value from a token
        /// </summary>
        private object ParseValue(object value)
        {
            var strValue = value.ToString();
            
            if (int.TryParse(strValue, out int intValue))
                return intValue;
            if (float.TryParse(strValue, out float floatValue))
                return floatValue;
            if (strValue.StartsWith("\"") && strValue.EndsWith("\""))
                return strValue.Substring(1, strValue.Length - 2);
            if (strValue.StartsWith("$"))
                return GetVariable(strValue.Substring(1));
            
            return strValue;
        }

        // Built-in function implementations
        
        private object ShowTextBox(object[] args)
        {
            if (args.Length > 0)
            {
                Console.WriteLine($"Show text box: {args[0]}");
            }
            return null;
        }

        private object HideTextBox(object[] args)
        {
            Console.WriteLine("Hide text box");
            return null;
        }

        private object WaitForTextBox(object[] args)
        {
            Console.WriteLine("Wait for text box");
            return null;
        }

        private object ShowString(object[] args)
        {
            if (args.Length > 0)
            {
                Console.WriteLine($"Show string: {args[0]}");
            }
            return null;
        }

        private object HideString(object[] args)
        {
            Console.WriteLine("Hide string");
            return null;
        }

        private object SetVariable(object[] args)
        {
            if (args.Length >= 2)
            {
                var varName = args[0].ToString();
                globalVariables[varName] = args[1];
                Console.WriteLine($"Set variable {varName} = {args[1]}");
            }
            return null;
        }

        private object GetVariable(object[] args)
        {
            if (args.Length > 0)
            {
                var varName = args[0].ToString();
                if (globalVariables.ContainsKey(varName))
                {
                    return globalVariables[varName];
                }
            }
            return 0;
        }

        private object SetHeroStat(object[] args)
        {
            if (args.Length >= 3)
            {
                var heroId = Convert.ToInt32(args[0]);
                var statName = args[1].ToString();
                var value = Convert.ToInt32(args[2]);
                Console.WriteLine($"Set hero {heroId} {statName} = {value}");
            }
            return null;
        }

        private object GetHeroStat(object[] args)
        {
            if (args.Length >= 2)
            {
                var heroId = Convert.ToInt32(args[0]);
                var statName = args[1].ToString();
                Console.WriteLine($"Get hero {heroId} {statName}");
                return 100; // Placeholder value
            }
            return 0;
        }

        private object TeleportToMap(object[] args)
        {
            if (args.Length >= 3)
            {
                var mapId = Convert.ToInt32(args[0]);
                var x = Convert.ToInt32(args[1]);
                var y = Convert.ToInt32(args[2]);
                Console.WriteLine($"Teleport to map {mapId} at ({x}, {y})");
            }
            return null;
        }

        private object TeleportToPosition(object[] args)
        {
            if (args.Length >= 2)
            {
                var x = Convert.ToInt32(args[0]);
                var y = Convert.ToInt32(args[1]);
                Console.WriteLine($"Teleport to position ({x}, {y})");
            }
            return null;
        }

        private object MoveHero(object[] args)
        {
            if (args.Length >= 2)
            {
                var direction = args[0].ToString();
                var distance = Convert.ToInt32(args[1]);
                Console.WriteLine($"Move hero {direction} by {distance}");
            }
            return null;
        }

        private object SetHeroDirection(object[] args)
        {
            if (args.Length > 0)
            {
                var direction = args[0].ToString();
                Console.WriteLine($"Set hero direction to {direction}");
            }
            return null;
        }

        private object GiveItem(object[] args)
        {
            if (args.Length >= 2)
            {
                var itemId = Convert.ToInt32(args[0]);
                var count = Convert.ToInt32(args[1]);
                Console.WriteLine($"Give item {itemId} x{count}");
            }
            return null;
        }

        private object TakeItem(object[] args)
        {
            if (args.Length >= 2)
            {
                var itemId = Convert.ToInt32(args[0]);
                var count = Convert.ToInt32(args[1]);
                Console.WriteLine($"Take item {itemId} x{count}");
            }
            return null;
        }

        private object CheckItem(object[] args)
        {
            if (args.Length > 0)
            {
                var itemId = Convert.ToInt32(args[0]);
                Console.WriteLine($"Check item {itemId}");
                return 1; // Placeholder - has item
            }
            return 0;
        }

        private object SetItemCount(object[] args)
        {
            if (args.Length >= 2)
            {
                var itemId = Convert.ToInt32(args[0]);
                var count = Convert.ToInt32(args[1]);
                Console.WriteLine($"Set item {itemId} count to {count}");
            }
            return null;
        }

        private object StartBattle(object[] args)
        {
            if (args.Length > 0)
            {
                var formationId = Convert.ToInt32(args[0]);
                Console.WriteLine($"Start battle with formation {formationId}");
            }
            return null;
        }

        private object EndBattle(object[] args)
        {
            Console.WriteLine("End battle");
            return null;
        }

        private object SetEnemyStat(object[] args)
        {
            if (args.Length >= 3)
            {
                var enemyId = Convert.ToInt32(args[0]);
                var statName = args[1].ToString();
                var value = Convert.ToInt32(args[2]);
                Console.WriteLine($"Set enemy {enemyId} {statName} = {value}");
            }
            return null;
        }

        private object ChangeEnemySprite(object[] args)
        {
            if (args.Length >= 2)
            {
                var enemyId = Convert.ToInt32(args[0]);
                var spriteId = Convert.ToInt32(args[1]);
                Console.WriteLine($"Change enemy {enemyId} sprite to {spriteId}");
            }
            return null;
        }

        private object PlayMusic(object[] args)
        {
            if (args.Length > 0)
            {
                var musicId = Convert.ToInt32(args[0]);
                Console.WriteLine($"Play music {musicId}");
            }
            return null;
        }

        private object StopMusic(object[] args)
        {
            Console.WriteLine("Stop music");
            return null;
        }

        private object PlaySound(object[] args)
        {
            if (args.Length > 0)
            {
                var soundId = Convert.ToInt32(args[0]);
                Console.WriteLine($"Play sound {soundId}");
            }
            return null;
        }

        private object SetVolume(object[] args)
        {
            if (args.Length >= 2)
            {
                var channel = args[0].ToString();
                var volume = Convert.ToInt32(args[1]);
                Console.WriteLine($"Set {channel} volume to {volume}");
            }
            return null;
        }

        private object ShowMenu(object[] args)
        {
            if (args.Length > 0)
            {
                var menuType = args[0].ToString();
                Console.WriteLine($"Show {menuType} menu");
            }
            return null;
        }

        private object HideMenu(object[] args)
        {
            Console.WriteLine("Hide menu");
            return null;
        }

        private object SetMenuOption(object[] args)
        {
            if (args.Length >= 3)
            {
                var menuId = Convert.ToInt32(args[0]);
                var optionId = Convert.ToInt32(args[1]);
                var enabled = Convert.ToBoolean(args[2]);
                Console.WriteLine($"Set menu {menuId} option {optionId} enabled = {enabled}");
            }
            return null;
        }

        private object IfStatement(object[] args)
        {
            if (args.Length > 0)
            {
                var condition = Convert.ToBoolean(args[0]);
                Console.WriteLine($"If statement: {condition}");
                return condition;
            }
            return false;
        }

        private object ElseStatement(object[] args)
        {
            Console.WriteLine("Else statement");
            return null;
        }

        private object EndIfStatement(object[] args)
        {
            Console.WriteLine("End if statement");
            return null;
        }

        private object WhileLoop(object[] args)
        {
            if (args.Length > 0)
            {
                var condition = Convert.ToBoolean(args[0]);
                Console.WriteLine($"While loop: {condition}");
                return condition;
            }
            return false;
        }

        private object EndWhileLoop(object[] args)
        {
            Console.WriteLine("End while loop");
            return null;
        }

        private object BreakStatement(object[] args)
        {
            Console.WriteLine("Break statement");
            return null;
        }

        private object ContinueStatement(object[] args)
        {
            Console.WriteLine("Continue statement");
            return null;
        }

        private object Add(object[] args)
        {
            if (args.Length >= 2)
            {
                var a = Convert.ToDouble(args[0]);
                var b = Convert.ToDouble(args[1]);
                return a + b;
            }
            return 0.0;
        }

        private object Subtract(object[] args)
        {
            if (args.Length >= 2)
            {
                var a = Convert.ToDouble(args[0]);
                var b = Convert.ToDouble(args[1]);
                return a - b;
            }
            return 0.0;
        }

        private object Multiply(object[] args)
        {
            if (args.Length >= 2)
            {
                var a = Convert.ToDouble(args[0]);
                var b = Convert.ToDouble(args[1]);
                return a * b;
            }
            return 0.0;
        }

        private object Divide(object[] args)
        {
            if (args.Length >= 2)
            {
                var a = Convert.ToDouble(args[0]);
                var b = Convert.ToDouble(args[1]);
                if (b != 0)
                    return a / b;
            }
            return 0.0;
        }

        private object Modulo(object[] args)
        {
            if (args.Length >= 2)
            {
                var a = Convert.ToInt32(args[0]);
                var b = Convert.ToInt32(args[1]);
                if (b != 0)
                    return a % b;
            }
            return 0;
        }

        private object Random(object[] args)
        {
            if (args.Length >= 2)
            {
                var min = Convert.ToInt32(args[0]);
                var max = Convert.ToInt32(args[1]);
                var random = new Random();
                return random.Next(min, max + 1);
            }
            return 0;
        }

        private object StringLength(object[] args)
        {
            if (args.Length > 0)
            {
                var str = args[0].ToString();
                return str.Length;
            }
            return 0;
        }

        private object Substring(object[] args)
        {
            if (args.Length >= 3)
            {
                var str = args[0].ToString();
                var start = Convert.ToInt32(args[1]);
                var length = Convert.ToInt32(args[2]);
                if (start >= 0 && start < str.Length && length > 0)
                {
                    var end = Math.Min(start + length, str.Length);
                    return str.Substring(start, end - start);
                }
            }
            return "";
        }

        private object StringEquals(object[] args)
        {
            if (args.Length >= 2)
            {
                var str1 = args[0].ToString();
                var str2 = args[1].ToString();
                return str1.Equals(str2, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        private object Concatenate(object[] args)
        {
            var result = new StringBuilder();
            foreach (var arg in args)
            {
                result.Append(arg.ToString());
            }
            return result.ToString();
        }

        /// <summary>
        /// Define a user function
        /// </summary>
        public void DefineFunction(string name, ScriptNode body)
        {
            userFunctions[name.ToLower()] = new ScriptFunctionDefinition
            {
                Name = name,
                Body = body
            };
        }

        /// <summary>
        /// Check if script engine is initialized
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Get global variable
        /// </summary>
        public object GetVariable(string name)
        {
            if (globalVariables.ContainsKey(name))
            {
                return globalVariables[name];
            }
            return null;
        }

        /// <summary>
        /// Set global variable
        /// </summary>
        public void SetVariable(string name, object value)
        {
            globalVariables[name] = value;
        }
    }

    /// <summary>
    /// Script function delegate
    /// </summary>
    public delegate object ScriptFunction(object[] arguments);

    /// <summary>
    /// Script token
    /// </summary>
    public class ScriptToken
    {
        public object Value { get; set; }
        public TokenType Type { get; set; }
    }

    /// <summary>
    /// Token types
    /// </summary>
    public enum TokenType
    {
        Identifier,
        Number,
        String,
        Variable,
        Parenthesis,
        Bracket,
        Comma
    }

    /// <summary>
    /// Script node for AST
    /// </summary>
    public class ScriptNode
    {
        public NodeType Type { get; set; }
        public object Value { get; set; }
        public List<ScriptNode> Children { get; set; } = new List<ScriptNode>();
    }

    /// <summary>
    /// Node types
    /// </summary>
    public enum NodeType
    {
        Root,
        FunctionCall,
        Value,
        Expression,
        Statement
    }

    /// <summary>
    /// Script function definition
    /// </summary>
    public class ScriptFunctionDefinition
    {
        public string Name { get; set; }
        public ScriptNode Body { get; set; }
    }

    /// <summary>
    /// Script execution context
    /// </summary>
    public class ScriptContext
    {
        public string FunctionName { get; set; }
        public object[] Arguments { get; set; }
        public Dictionary<string, object> LocalVariables { get; set; }
    }
}
