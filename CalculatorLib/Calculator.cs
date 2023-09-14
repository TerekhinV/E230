namespace CalculatorLib;
using System;
using System.Diagnostics;
public class Calculator{
    public Calculator(){
        StreamWriter logFile = File.CreateText("calc.log");
        Trace.Listeners.Add(new TextWriterTraceListener(logFile));
        Trace.AutoFlush = true;
        Trace.WriteLine("Begin calculator log");
        Trace.WriteLine(String.Format("Started {0}", System.DateTime.Now.ToString()));
    }
    public double doOp(double a, double b, string op){
        double res;
        try{
            switch (op){
                case "+":       res = a+b; break;
                case "-":       res = a-b; break;
                case "*":       res = a*b; break;
                case "/":       res = a/b; break;
                case "^":       res = Math.Pow(a, b); break;
                case "root":    res = Math.Pow(a, 1/b); break;
                default:        Trace.WriteLine("Operation not supported"); return double.NaN;
            }
            Trace.WriteLine(String.Format("{0} {3} {1} = {2}", a, b, res, op));
            return res;
        }catch{return double.NaN;}
    }
}
