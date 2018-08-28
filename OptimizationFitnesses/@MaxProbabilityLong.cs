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
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

namespace NinjaTrader.NinjaScript.OptimizationFitnesses
{
	public class MaxProbablityLong : OptimizationFitness
	{
		protected override void OnCalculatePerformanceValue(StrategyBase strategy)
		{
			if (strategy.SystemPerformance.LongTrades.TradesCount <= 1 || strategy.SystemPerformance.LongTrades.TradesPerformance.Percent.AverageProfit == 0)
				Value = 0;
			else
			{
				double div	= strategy.SystemPerformance.LongTrades.TradesPerformance.Percent.StdDev / Math.Sqrt(strategy.SystemPerformance.LongTrades.TradesCount);
				double t	= Core.Stat.StudTP(strategy.SystemPerformance.LongTrades.TradesPerformance.Percent.AverageProfit / div, strategy.SystemPerformance.LongTrades.TradesCount - 1);
				Value = (div <= 0.5 ? 1 - t : t);
			}
		}

		protected override void OnStateChange()
		{               
			if (State == State.SetDefaults)
				Name = NinjaTrader.Custom.Resource.NinjaScriptOptimizationFitnessNameMaxProbablityLong;
		}
	}
}
