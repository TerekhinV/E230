namespace CalculatorLib;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
public class Calculator{
    public Calculator(){
    }
    public static double doOp(double a, double b, string op){
        double res;
        try{
            switch (op){
                case "+":       res = a+b; break;
                case "-":       res = a-b; break;
                case "*":       res = a*b; break;
                case "/":       res = a/b; break;
                case "^":       res = Math.Pow(a, b); break;
                case "√":       res = Math.Pow(b, 1 / a); break;
                case "neg":     res = -a; break;
                case "sqrt":    res = Math.Sqrt(a); break;
                case "sin":     res = Math.Sin(a); break;
                case "cos":     res = Math.Cos(a); break;
                case "tan":     res = Math.Tan(a); break;
                case "abs":     res = Math.Abs(a); break;
                default:        return double.NaN;
            }
            return res;
        }catch{return double.NaN;}
    }
    public static double evaluate(string eq){
        //                         //split   symbols   numbers           constants//
        Regex tokenizer = new Regex("(?<=[\\-)(+=/*^ ]|[0-9.]+(?![0-9.])|pi|e)"); //ooh boy here we go: regex to split equation string by token. groups numbers, function names, constants, and/or individual symbols from there
        List<string> tokens = new List<string>(tokenizer.Split(eq));

        bool expValue = true; //setup things to do evaluating
        Stack<string> ops = new Stack<string>();
        Stack<double> vals = new Stack<double>();
        string op;

        foreach (string token in tokens){
            if (expValue){
                switch (token){
                    case " ": case "": break; //remove spaces
                    case "-": ops.Push("neg"); break; //negator operator to handle unary negation
                    case "pi":
                        vals.Push(Math.PI);
                        expValue = false;
                        break;
                    case "e":
                        vals.Push(Math.E);
                        expValue = false;
                        break;
                    default:
                        double val;
                        if (Double.TryParse(token, out val)){ //actual numbers
                            vals.Push(val);
                            expValue = false;
                        } else if (token.EndsWith("(")){ //functions, subexpressions
                            ops.Push(token);
                        }else{ //unexpected
                            throw new Exception("Failed to parse equation: expected a value, function, or subexpression, instead found '" + token + "'");
                        }
                        break;
                }
            }else
            {
                switch (token)
                {
                    case " ": case "": break; //remove spaces
                    case ")": //handle end of subexpression or function
                        while (ops.TryPeek(out op) && !op.EndsWith("("))
                        {
                            collapse(ops.Pop(), vals);
                        }
                        if (ops.TryPop(out op))
                        {
                            if (op.Length > 1) collapse(op.Remove(op.Length - 1), vals);
                        } else throw new Exception("Failed to parse equation: unmatched closing bracket");
                        break; 
                    case "+": //normal operators
                    case "-":
                    case "*":
                    case "/":
                    case "^":
                        while (ops.TryPeek(out op) && precedes(token, op))
                        {
                            collapse(ops.Pop(), vals);
                        }
                         ops.Push(token); expValue = true;
                        break;
                    default:
                        throw new Exception("Failed to parse equation: expected an operator or end of subexpression, instead found '" + token + "'");
                }
            }
        }
        while (ops.TryPeek(out op)) //final collapse
        {
            collapse(ops.Pop(), vals);
        }
        double res;
        if (vals.TryPop(out res))
        {
            if (vals.Count == 0)
            {
                if (ops.Count == 0)
                {
                    return res;
                }
                else throw new Exception("Failed to evaluate equation: leftover operators in stack");
            }
            else throw new Exception("Failed to evaluate equation: too many leftover values in stack");
        }
        else throw new Exception("Failed to evaluate equation: no result value in stack");
    }
    static bool precedes(string a, string? b)// collapse first -> neg -> ^ -> *|/ -> +|- -> () -> <null> -> collapse last. True if b is same tier or earlier than a.
    {                                        // note that this function won't actually ever see a bunch of these so we can compress it a bit
        switch (a)
        {
            case "^": return (new string[] { "neg", "^" }).Contains(b);
            case "*": case "/": return (new string[] { "neg", "^", "*", "/" }).Contains(b);
            case "+": case "-": return (new string[] { "neg", "^", "*", "/", "+", "-" }).Contains(b);
            default: throw new Exception("Failed to check operator precedence: unsupported symbol "+a);
        }
    }
    static void collapse(string op, Stack<double> vals)
    {
        Stack<double> buffer = new Stack<double>();
        double buf;
        op = (op.EndsWith("("))? op.Remove(op.Length-1):op; //remove parenthese from functions for consistency
        switch (op)
        {
            case "neg":
            case "sqrt":
            case "sin":
            case "cos":
            case "tan":
            case "abs":
                if (!vals.TryPop(out buf)) throw new Exception("failed to complete operation '" + op + "': insufficient operands"); //preeeeety sure any insufficient operand problems should be caught beforehand but in case they're not here's a backup
                vals.Push(doOp(buf, 0, op));
                break;
            case "+":
            case "-":
            case "*":
            case "/":
            case "^":
                if (!vals.TryPop(out buf)) throw new Exception("failed to complete operation '" + op + "': insufficient operands");
                buffer.Push(buf);
                if (!vals.TryPop(out buf)) throw new Exception("failed to complete operation '" + op + "': insufficient operands");
                vals.Push(doOp(buf, buffer.Pop(), op));
                break;
            default:
                throw new Exception("failed to complete operation: unsupported operator/function '" + op + "'");
        }
    }
}
