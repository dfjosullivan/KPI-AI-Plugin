#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using System.IO;
using System.Globalization;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class ShowSwingChangePattern : Indicator
	{
	    private double iOffset = 0;
	    private int iOffsetTicks = 1;
		
 		private string productVersion = " v1.0.3"; 
 		private string productName = "Show Swing Change Pattern";
		private string PeriodSwingString = "";
		private string FileDirectoryString = "";
		private string SpanString = "";
	    private string BarsString = "";
		
		const int Xdpos = 5;
        const int Xpos = 9;
		const int Adpos = 6;
		const int Apos = 10;
	    const int Bdpos = 7;
	    const int Bpos = 11;
	    const int Odpos = 8;
	    const int Opos = 12;

		const int stoppos = 17;
	    const int Exitdpos = 19;
	    const int Exitpos = 20;
	    const int Profitpos = 22;

		const int swingDatePos = 3;
		const int swingPricePos = 1;
		
        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Displays the swing changes for historical data in a provided file.";
				Name										= string.Format("{0} {1}", productName, productVersion); 
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				Span										= 3;
				PeriodSwing									= 3;
				FileDirectory								= "C:\\Working";
				AddPlot(new Stroke(Brushes.Orange, 2), PlotStyle.Dot, "Example");
				ShowSwings									= false;
			}
			else if (State == State.Configure)
			{
			} 
			else if (State == State.DataLoaded)
			{
				PeriodSwingString = PeriodSwing.ToString();
				FileDirectoryString = FileDirectory.ToString();
				SpanString = Span.ToString();
			    BarsString = Bars.BarsPeriod.Value.ToString();
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
			if (CurrentBar < 0)
                return;

            if (IsFirstTickOfBar)
            {
                if (CurrentBar == (Count -2))
                {
					string path = string.Concat(FileDirectoryString, "\\YM_swing_", BarsString, "_ticks_", SpanString, "_span_", PeriodSwingString, "_period.csv");
                    if (!File.Exists(path))
                    {
                        System.Windows.Forms.MessageBox.Show(string.Concat(path, " does not exist"));
						return;
                    }
                    string pathSwings = string.Concat(FileDirectoryString, "\\YM_swing_plot_", BarsString, "_ticks_", SpanString, "_span_", PeriodSwingString, "_period.csv");
                    if (!File.Exists(path))
                    {
                        System.Windows.Forms.MessageBox.Show(string.Concat(pathSwings, " does not exist"));
						return;
                    }
    
					
					var reader = new StreamReader(File.OpenRead(pathSwings));
		            List<string> searchList = new List<string>();
                    int i = 0;
					int j = 0;
					DateTime swingDateN0 = new DateTime();
					double swingPriceN0 = 0;
		            while(!reader.EndOfStream && ShowSwings) {
						var line = reader.ReadLine();
						string[] values = line.Split(',');
						
						if (i == 0)
                        {
							//headers
                            i++;
                            continue;
                        }
						
						
						string swingDateText = values[swingDatePos];
		                DateTime swingDateN1 = DateTime.ParseExact(swingDateText.Substring(0, swingDateText.Length - 4), "yyyyMMdd HHmmss fff", System.Globalization.CultureInfo.InvariantCulture);
						double swingPriceN1 = Double.Parse(values[swingPricePos]);
						
						if (i == 1)
                        {
							swingDateN0 = swingDateN1;
							swingPriceN0 =  swingPriceN1;
                            i++;
                            continue;
                        }
							
						Draw.Line(this, swingDateN0.ToString() + swingDateN1.ToString(), true, swingDateN0, swingPriceN0, swingDateN1, swingPriceN1, Brushes.White,
		                        DashStyleHelper.DashDot, 1, true);
						
						
						swingDateN0 = swingDateN1;
						swingPriceN0 =  swingPriceN1;
						
						i++;
						
					}
					
					
					reader = new StreamReader(File.OpenRead(path));
		            searchList = new List<string>();
                    i = 0;
					j = 0;
		            while(!reader.EndOfStream)
		            {
		                var line = reader.ReadLine();
                        if (i == 0)
                        {
                            i++;
                            continue;
                        }
							
		                
						string[] values = line.Split(',');
						string X_d = values[Xdpos];
		                DateTime X_d_date = DateTime.ParseExact(X_d.Substring(0, X_d.Length - 4), "yyyyMMdd HHmmss fff", System.Globalization.CultureInfo.InvariantCulture);
                        double X = Double.Parse(values[Xpos]);
						string A_d = values[Adpos];
		                double A = Double.Parse(values[Apos]);
		                DateTime A_d_date = DateTime.ParseExact(A_d.Substring(0, A_d.Length - 4), "yyyyMMdd HHmmss fff", System.Globalization.CultureInfo.InvariantCulture);
		                string B_d = values[Bdpos];
		                double B = Double.Parse(values[Bpos]);
		                DateTime B_d_date = DateTime.ParseExact(B_d.Substring(0, B_d.Length - 4), "yyyyMMdd HHmmss fff", System.Globalization.CultureInfo.InvariantCulture);
		                string O_d = values[Odpos];
                        double O = Double.Parse(values[Opos]);
		                DateTime O_d_date = DateTime.ParseExact(O_d.Substring(0, O_d.Length - 4), "yyyyMMdd HHmmss fff", System.Globalization.CultureInfo.InvariantCulture);
		                string Exit_d = values[Exitdpos];
		                double Exit = Double.Parse(values[Exitpos]);
		                DateTime Exit_d_date = DateTime.ParseExact(Exit_d.Substring(0, Exit_d.Length - 4), "yyyyMMdd HHmmss fff", System.Globalization.CultureInfo.InvariantCulture);
		                double profit = Double.Parse(values[Profitpos]);
						double stopValue = Double.Parse(values[stoppos]);
                        
		                if (profit > 0)
		                {
		                    Draw.Line(this, X_d + A_d, true, X_d_date, X, A_d_date, A, Brushes.Blue,
		                        DashStyleHelper.Solid, 3, true);
							//Draw.Text(this,B_d, i.ToString(), B_d_date, B, Brushes.Gold); 
		                    Draw.Line(this, A_d + B_d, true, A_d_date, A, B_d_date, B, Brushes.Blue,
		                        DashStyleHelper.Solid, 3, true);
                            Draw.Line(this, O_d + Exit_d, true, O_d_date, O, Exit_d_date, Exit, Brushes.Blue,
		                       DashStyleHelper.Dot, 1, true);
							Draw.Text(this,O_d  + "B"  + j.ToString(), true,   j.ToString(), B_d_date, B, 5, Brushes.White, new SimpleFont(), TextAlignment.Center, Brushes.Black, Brushes.Black, 50); 
							
							//Draw.Text(this,O_d  + "label", "Open"  + i.ToString(), O_d_date, O, Brushes.Gold); 
							//Draw.Text(this,Exit_d + "label", "Close"  + i.ToString(), Exit_d_date, Exit, Brushes.Gold); 
							Draw.Diamond(this,O_d  +  O_d_date + O + "Diamond"  + j.ToString(), true, O_d_date, O, Brushes.Blue); 
							Draw.Diamond(this,O_d  +  Exit_d_date + Exit + "Diamond"  + j.ToString(), true, Exit_d_date, Exit, Brushes.Blue); 
							
							Draw.Text(this,O_d  + "label" + "Open"  + j.ToString(), true,  "Open "  + j.ToString(), O_d_date, O, 5, Brushes.LightCoral, new SimpleFont(), TextAlignment.Center, Brushes.Black, Brushes.Black, 50); 
							Draw.Text(this,O_d  + "label" + "Close"  + j.ToString() , true,  "Close "  +j.ToString() + " {" +"Profit " + profit.ToString() + "}", Exit_d_date, Exit, 5, Brushes.LightCoral, new SimpleFont(), TextAlignment.Center, Brushes.Black, Brushes.Black, 50); 
							
		                }
		                else
		                {
		                    Draw.Line(this, X_d + A_d, true, X_d_date, X, A_d_date, A, Brushes.Orange,
		                        DashStyleHelper.Solid, 3, true);
                            Draw.Line(this, A_d + B_d, true, A_d_date, A, B_d_date, B, Brushes.Orange,
                                DashStyleHelper.Solid, 3, true);
							//Draw.Text(this,B_d, i.ToString(), B_d_date, B, Brushes.White);
							Draw.Text(this,O_d  + "B"  + j.ToString(), true,   j.ToString(), B_d_date, B, 5, Brushes.White, new SimpleFont(), TextAlignment.Center, Brushes.Black, Brushes.Black, 50); 
							
                            Draw.Line(this, O_d + Exit_d, true, O_d_date, O, Exit_d_date, Exit, Brushes.LightCoral,
		                        DashStyleHelper.Dot, 1, true);
							Draw.Diamond(this,O_d  +  O_d_date + O + "Diamond"  + i.ToString(), true, O_d_date, O, Brushes.Orange); 
							Draw.Diamond(this,O_d  +  Exit_d_date + Exit + "Diamond"  + i.ToString(), true, Exit_d_date, Exit, Brushes.Orange); 
							Draw.Text(this,O_d  + "label" + "Open"  + j.ToString(), true,  "Open "  + j.ToString(), O_d_date, O, 5, Brushes.Orange, new SimpleFont(), TextAlignment.Center, Brushes.Black, Brushes.Black, 50); 
							Draw.Text(this,O_d  + "label" + "Close"  + j.ToString(), true,  "Close "  + j.ToString() + " {" +"Loss " + profit.ToString() + "}", Exit_d_date, Exit, 5, Brushes.Orange, new SimpleFont(), TextAlignment.Center, Brushes.Black, Brushes.Black, 50); 
							//Draw.Text(this, Exit_d + "label", "Close"  + i.ToString(), Exit_d_date, Exit, Brushes.LightCoral); 
                        }
						
						//Draw.Line(this, "stop" +i.ToString() , true, B_d_date , stopValue, Exit_d_date, stopValue, Brushes.Red,
                        //        DashStyleHelper.Solid, 1, true);
						//raw.Text(this, Exit_d + "stop", "Original Stop " + i, B_d_date + 5, stopValue, Brushes.Red); 
						//Draw.Text(this,O_d  + "label" + "Stop"  + j.ToString(), true,  "Stop "  + j.ToString(), Exit_d_date, stopValue, 5, Brushes.LightCoral, new SimpleFont(), TextAlignment.Center, Brushes.Black, Brushes.Black, 50); 
							
						j++;
		            }
					
					
						
					
                }


            }
			
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name="Span", Description="2 or 3", Order=1, GroupName="Parameters")]
		public int Span
		{ get; set; }

		[NinjaScriptProperty]
		[Range(3, int.MaxValue)]
		[Display(Name="PeriodSwing", Description="3, 5, or 7", Order=2, GroupName="Parameters")]
		public int PeriodSwing
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show Swings", Description="Show the swings", Order=3, GroupName="Parameters")]
		public bool ShowSwings
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="FileDirectory", Description="Folder where files can be loaded", Order=2, GroupName="Parameters")]
		public string FileDirectory
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Example
		{
			get { return Values[0]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ShowSwingChangePattern[] cacheShowSwingChangePattern;
		public ShowSwingChangePattern ShowSwingChangePattern(int span, int periodSwing, bool showSwings, string fileDirectory)
		{
			return ShowSwingChangePattern(Input, span, periodSwing, showSwings, fileDirectory);
		}

		public ShowSwingChangePattern ShowSwingChangePattern(ISeries<double> input, int span, int periodSwing, bool showSwings, string fileDirectory)
		{
			if (cacheShowSwingChangePattern != null)
				for (int idx = 0; idx < cacheShowSwingChangePattern.Length; idx++)
					if (cacheShowSwingChangePattern[idx] != null && cacheShowSwingChangePattern[idx].Span == span && cacheShowSwingChangePattern[idx].PeriodSwing == periodSwing && cacheShowSwingChangePattern[idx].ShowSwings == showSwings && cacheShowSwingChangePattern[idx].FileDirectory == fileDirectory && cacheShowSwingChangePattern[idx].EqualsInput(input))
						return cacheShowSwingChangePattern[idx];
			return CacheIndicator<ShowSwingChangePattern>(new ShowSwingChangePattern(){ Span = span, PeriodSwing = periodSwing, ShowSwings = showSwings, FileDirectory = fileDirectory }, input, ref cacheShowSwingChangePattern);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ShowSwingChangePattern ShowSwingChangePattern(int span, int periodSwing, bool showSwings, string fileDirectory)
		{
			return indicator.ShowSwingChangePattern(Input, span, periodSwing, showSwings, fileDirectory);
		}

		public Indicators.ShowSwingChangePattern ShowSwingChangePattern(ISeries<double> input , int span, int periodSwing, bool showSwings, string fileDirectory)
		{
			return indicator.ShowSwingChangePattern(input, span, periodSwing, showSwings, fileDirectory);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ShowSwingChangePattern ShowSwingChangePattern(int span, int periodSwing, bool showSwings, string fileDirectory)
		{
			return indicator.ShowSwingChangePattern(Input, span, periodSwing, showSwings, fileDirectory);
		}

		public Indicators.ShowSwingChangePattern ShowSwingChangePattern(ISeries<double> input , int span, int periodSwing, bool showSwings, string fileDirectory)
		{
			return indicator.ShowSwingChangePattern(input, span, periodSwing, showSwings, fileDirectory);
		}
	}
}

#endregion
