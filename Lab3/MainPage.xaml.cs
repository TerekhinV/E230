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
        
    }
    private void Evaluate(object s, EventArgs e)
    {
        if (inp.Text == "42")
        {
            output.Text = "The Answer"; return;
        }
        try
        {
            output.Text = Calculator.evaluate(inp.Text).ToString();
        }
        catch (Exception ex) { output.Text = ex.Message; }
    }
}