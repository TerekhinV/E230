namespace CalculatorLib;
using System;
using System.Diagnostics;
using Newtonsoft.Json;
public class Calculator{
    JsonWriter writer;
    public Calculator(){
        StreamWriter logFile = File.CreateText("calculatorlog.json");
        logFile.AutoFlush = true;
        writer = new JsonTextWriter(logFile);
        writer.Formatting = Formatting.Indented;
        writer.WriteStartObject();
        writer.WritePropertyName("Operations");
        writer.WriteStartArray();
    }
    public void Finish(){
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Close();
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
                default:        return double.NaN;
            }
            writer.WriteStartObject();
            writer.WritePropertyName("Operand1");
            writer.WriteValue(a);
            writer.WritePropertyName("Operand2");
            writer.WriteValue(b);
            writer.WritePropertyName("Operation");
            writer.WriteValue(op);
            writer.WritePropertyName("Result");
            writer.WriteValue(res);
            writer.WriteEndObject();
            return res;
        }catch{return double.NaN;}
    }
}
