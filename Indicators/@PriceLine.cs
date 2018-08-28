//
// Copyright (C) 2018, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Displays ask, bid, and/or last lines on the chart.
	/// </summary>
	public class PriceLine : Indicator
	{
		private double 					ask;
		private double 					bid;
		private double 					last;
		private SharpDX.Direct2D1.Brush askBrush;
		private SharpDX.Direct2D1.Brush bidBrush;
		private SharpDX.Direct2D1.Brush lastBrush;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description						= NinjaTrader.Custom.Resource.NinjaScriptIndicatorDescriptionPriceLine;
				Name							= NinjaTrader.Custom.Resource.NinjaScriptIndicatorNamePriceLine;
				Calculate						= Calculate.OnPriceChange;
				IsOverlay						= true;
				ShowTransparentPlotsInDataBox	= false;
				DrawOnPricePanel				= true;
				IsSuspendedWhileInactive 		= true;
				ShowAskLine 					= false;
				ShowBidLine 					= false;
				ShowLastLine 					= true;
				AskLineLength 					= 100;
				BidLineLength 					= 100;
				LastLineLength 					= 100;
				AskStroke						= new Stroke(Brushes.DarkGreen, DashStyleHelper.Dash, 1);
				BidStroke						= new Stroke(Brushes.Blue, DashStyleHelper.Dash, 1);
				LastStroke						= new Stroke(Brushes.Yellow, DashStyleHelper.Dash, 1);
			}
			else if (State == State.Configure)
			{
				AddPlot(ShowAskLine ?	AskStroke.Brush :	Brushes.Transparent, NinjaTrader.Custom.Resource.PriceLinePlotAsk);
				AddPlot(ShowBidLine ?	BidStroke.Brush :	Brushes.Transparent, NinjaTrader.Custom.Resource.PriceLinePlotBid);
				AddPlot(ShowLastLine ?	LastStroke.Brush :	Brushes.Transparent, NinjaTrader.Custom.Resource.PriceLinePlotLast);
			}
		}

		protected override void OnBarUpdate() { }
		
		public override void OnCalculateMinMax()
		{
			double tmpMin = double.MaxValue;
			double tmpMax = double.MinValue;
			
			if (ShowAskLine && ask != double.MinValue)
			{
				tmpMin = Math.Min(tmpMin, ask);
				tmpMax = Math.Max(tmpMax, ask);
			}
			
			if (ShowBidLine && bid != double.MinValue)
			{
				tmpMin = Math.Min(tmpMin, bid);
				tmpMax = Math.Max(tmpMax, bid);
			}
			
			if (ShowLastLine && last != double.MinValue)
			{
				tmpMin = Math.Min(tmpMin, last);
				tmpMax = Math.Max(tmpMax, last);
			}
			
			MinValue = tmpMin;
			MaxValue = tmpMax;
		}
		
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Last)
			{
				Values[0][0] = ask	= e.Ask;
				Values[1][0] = bid	= e.Bid;
				Values[2][0] = last = e.Price;
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (BarsArray[0] == null || ChartBars == null)
				return;
			
			ChartPanel	panel 	= chartControl.ChartPanels[chartScale.PanelIndex];
			float 		endX 	= panel.X + panel.W;
			
			if (ShowAskLine && ask != double.MinValue)
			{
				float startX 	= Convert.ToSingle(panel.X + panel.W * (1 - (AskLineLength / 100.0)));
				float y 		= chartScale.GetYByValue(ask);
				
				SharpDX.Direct2D1.StrokeStyleProperties strokeProperties = new SharpDX.Direct2D1.StrokeStyleProperties() { DashStyle = AskStroke.DashStyleDX };
				
				using (SharpDX.Direct2D1.StrokeStyle strokeStyle = new SharpDX.Direct2D1.StrokeStyle(NinjaTrader.Core.Globals.D2DFactory, strokeProperties))
				{
					RenderTarget.DrawLine(new SharpDX.Vector2(startX, y), new SharpDX.Vector2(endX, y), askBrush, AskStroke.Width, strokeStyle);
				}
			}
			
			if (ShowBidLine && bid != double.MinValue)
			{
				float startX 	= Convert.ToSingle(panel.X + panel.W * (1 - (BidLineLength / 100.0)));
				float y 		= chartScale.GetYByValue(bid);
				
				SharpDX.Direct2D1.StrokeStyleProperties strokeProperties = new SharpDX.Direct2D1.StrokeStyleProperties() { DashStyle = BidStroke.DashStyleDX };
				
				using (SharpDX.Direct2D1.StrokeStyle strokeStyle = new SharpDX.Direct2D1.StrokeStyle(NinjaTrader.Core.Globals.D2DFactory, strokeProperties))
				{
					RenderTarget.DrawLine(new SharpDX.Vector2(startX, y), new SharpDX.Vector2(endX, y), bidBrush, BidStroke.Width, strokeStyle);
				}
			}
			
			if (ShowLastLine && last != double.MinValue)
			{
				float startX 	= Convert.ToSingle(panel.X + panel.W * (1 - (LastLineLength / 100.0)));
				float y 		= chartScale.GetYByValue(last);
				
				SharpDX.Direct2D1.StrokeStyleProperties strokeProperties = new SharpDX.Direct2D1.StrokeStyleProperties() { DashStyle = LastStroke.DashStyleDX };
				
				using (SharpDX.Direct2D1.StrokeStyle strokeStyle = new SharpDX.Direct2D1.StrokeStyle(NinjaTrader.Core.Globals.D2DFactory, strokeProperties))
				{
					RenderTarget.DrawLine(new SharpDX.Vector2(startX, y), new SharpDX.Vector2(endX, y), lastBrush, LastStroke.Width, strokeStyle);
				}
			}
		}
		
		public override void OnRenderTargetChanged()
		{
			if (askBrush != null)
				askBrush.Dispose();
			
			if (bidBrush != null)
				bidBrush.Dispose();
			
			if (lastBrush != null)
				lastBrush.Dispose();
			
			if (RenderTarget != null)
			{
				askBrush 	= AskStroke.Brush.ToDxBrush(RenderTarget);
				bidBrush 	= BidStroke.Brush.ToDxBrush(RenderTarget);
				lastBrush 	= LastStroke.Brush.ToDxBrush(RenderTarget);
			}
		}
		
		#region Properties
		[XmlIgnore]
		[Browsable(false)]
		public double AskLine {  get { return ask; } }

		[XmlIgnore]
		[Browsable(false)]
		public double BidLine {  get { return bid; } }

		[XmlIgnore]
		[Browsable(false)]
		public double LastLine {  get { return last; } }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "ShowAskLine", GroupName = "NinjaScriptParameters", Order = 0)]
		public bool ShowAskLine { get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "ShowBidLine", GroupName = "NinjaScriptParameters", Order = 1)]
		public bool ShowBidLine { get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "ShowLastLine", GroupName = "NinjaScriptParameters", Order = 2)]
		public bool ShowLastLine { get; set; }
		
		[Range(1, 100), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "AskLineLength", GroupName = "NinjaScriptParameters", Order = 3)]
		public int AskLineLength { get; set; }
		
		[Range(1, 100), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "BidLineLength", GroupName = "NinjaScriptParameters", Order = 4)]
		public int BidLineLength { get; set; }
		
		[Range(1, 100), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "LastLineLength", GroupName = "NinjaScriptParameters", Order = 5)]
		public int LastLineLength { get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "AskLineStroke", GroupName = "NinjaScriptParameters", Order = 6)]
		public Stroke AskStroke { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "BidLineStroke", GroupName = "NinjaScriptParameters", Order = 7)]
		public Stroke BidStroke { get; set; }

		[Display(ResourceType = typeof(Custom.Resource), Name = "LastLineStroke", GroupName = "NinjaScriptParameters", Order = 8)]
		public Stroke LastStroke { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PriceLine[] cachePriceLine;
		public PriceLine PriceLine(bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
		{
			return PriceLine(Input, showAskLine, showBidLine, showLastLine, askLineLength, bidLineLength, lastLineLength);
		}

		public PriceLine PriceLine(ISeries<double> input, bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
		{
			if (cachePriceLine != null)
				for (int idx = 0; idx < cachePriceLine.Length; idx++)
					if (cachePriceLine[idx] != null && cachePriceLine[idx].ShowAskLine == showAskLine && cachePriceLine[idx].ShowBidLine == showBidLine && cachePriceLine[idx].ShowLastLine == showLastLine && cachePriceLine[idx].AskLineLength == askLineLength && cachePriceLine[idx].BidLineLength == bidLineLength && cachePriceLine[idx].LastLineLength == lastLineLength && cachePriceLine[idx].EqualsInput(input))
						return cachePriceLine[idx];
			return CacheIndicator<PriceLine>(new PriceLine(){ ShowAskLine = showAskLine, ShowBidLine = showBidLine, ShowLastLine = showLastLine, AskLineLength = askLineLength, BidLineLength = bidLineLength, LastLineLength = lastLineLength }, input, ref cachePriceLine);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PriceLine PriceLine(bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
		{
			return indicator.PriceLine(Input, showAskLine, showBidLine, showLastLine, askLineLength, bidLineLength, lastLineLength);
		}

		public Indicators.PriceLine PriceLine(ISeries<double> input , bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
		{
			return indicator.PriceLine(input, showAskLine, showBidLine, showLastLine, askLineLength, bidLineLength, lastLineLength);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PriceLine PriceLine(bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
		{
			return indicator.PriceLine(Input, showAskLine, showBidLine, showLastLine, askLineLength, bidLineLength, lastLineLength);
		}

		public Indicators.PriceLine PriceLine(ISeries<double> input , bool showAskLine, bool showBidLine, bool showLastLine, int askLineLength, int bidLineLength, int lastLineLength)
		{
			return indicator.PriceLine(input, showAskLine, showBidLine, showLastLine, askLineLength, bidLineLength, lastLineLength);
		}
	}
}

#endregion
