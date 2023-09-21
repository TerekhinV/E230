namespace Lab3;
using CalculatorLib;
public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

    private void OperatorClicked(object s, EventArgs e)
    {
        op.Text = (s as Button).Text;
    }
    private void Evaluate(object s, EventArgs e)
    {
        double a, b, o;
        if (!double.TryParse(numA.Text, out a))
        {
            output.Text = "Failed to parse input A";
            return;
        }
        if (!double.TryParse(numB.Text, out b))
        {
            output.Text = "Failed to parse input B";
            return;
        }
        o = Calculator.doOp(a, b, op.Text);
        output.Text = double.IsNaN(o) ? "Operation error" : o.ToString();
    }
}