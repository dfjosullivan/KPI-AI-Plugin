﻿//
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
	/// The Disparity Index measures the difference between the price and an exponential moving average. A value greater could suggest bullish momentum, while a value less than zero could suggest bearish momentum.
	/// </summary>
	public class DisparityIndex : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= NinjaTrader.Custom.Resource.NinjaScriptIndicatorDescriptionDisparityIndex;
				Name						= NinjaTrader.Custom.Resource.NinjaScriptIndicatorNameDisparityIndex;
				IsOverlay					= false;
				IsSuspendedWhileInactive	= true;
				Period 						= 25;
				
				AddPlot(Brushes.DodgerBlue,   NinjaTrader.Custom.Resource.NinjaScriptIndicatorDisparityLine);
				AddLine(Brushes.DarkGray, 0,  NinjaTrader.Custom.Resource.NinjaScriptIndicatorZeroLine);
			}
		}

		protected override void OnBarUpdate()
		{
			Value[0] = 100 * (Close[0] - EMA(Close, Period)[0]) / Close[0];
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DisparityIndex[] cacheDisparityIndex;
		public DisparityIndex DisparityIndex(int period)
		{
			return DisparityIndex(Input, period);
		}

		public DisparityIndex DisparityIndex(ISeries<double> input, int period)
		{
			if (cacheDisparityIndex != null)
				for (int idx = 0; idx < cacheDisparityIndex.Length; idx++)
					if (cacheDisparityIndex[idx] != null && cacheDisparityIndex[idx].Period == period && cacheDisparityIndex[idx].EqualsInput(input))
						return cacheDisparityIndex[idx];
			return CacheIndicator<DisparityIndex>(new DisparityIndex(){ Period = period }, input, ref cacheDisparityIndex);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DisparityIndex DisparityIndex(int period)
		{
			return indicator.DisparityIndex(Input, period);
		}

		public Indicators.DisparityIndex DisparityIndex(ISeries<double> input , int period)
		{
			return indicator.DisparityIndex(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DisparityIndex DisparityIndex(int period)
		{
			return indicator.DisparityIndex(Input, period);
		}

		public Indicators.DisparityIndex DisparityIndex(ISeries<double> input , int period)
		{
			return indicator.DisparityIndex(input, period);
		}
	}
}

#endregion
