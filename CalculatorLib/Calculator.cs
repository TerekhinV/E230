namespace CalculatorLib;
using System;
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
                case "√":    res = Math.Pow(b, 1/a); break;
                default:        return double.NaN;
            }
            return res;
        }catch{return double.NaN;}
    }
}
