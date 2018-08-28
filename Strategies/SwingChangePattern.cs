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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class SwingChangePattern : Strategy
	{
		//Use odd number
		private const int searchArea = 5;
		private const int searchAreaLeft = 3;
		private const int searchAreaRight = 3;
		private const string MIN = "minimum";
		private const string MAX = "maximum";
		private int lastHistoricalBar;

		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Swing Change Pattern v1.0.2";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 40;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				SwingPeriod					= 3;
				AddLine(Brushes.White, 1, "SwingPeriodLine");
				AddLine(Brushes.Blue, 2, "BearSwingChange");
				AddLine(Brushes.Indigo, 2, "BullSwingChange");
				AddLine(Brushes.Orange, 1, "Trade");
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				List<int> allowedTicks = new List<int> {100,200,300,400,500};
			    List<string> allowedInstruments = new List<string> {"YM 09-18", "YM 12-18", "YM"};
				RemoveDrawObjects();
				if (!allowedTicks.Contains(Bars.BarsPeriod.Value)) {
					MessageBox.Show("Allowed tick count is 100, 200, 300, 400, 500 not " + Bars.BarsPeriod.Value.ToString());
					return;
				} 
				if (!allowedInstruments.Contains(Instrument.FullName)) {
					MessageBox.Show("Only YM or YM 09-18 or YM- 12-18 allowed, not " + Instrument.FullName);
					return;
				}

			    if (!Bars.BarsPeriod.MarketDataType.Equals(MarketDataType.Last))
			    {
			        MessageBox.Show("Last data only not " + Bars.BarsPeriod.MarketDataType.ToString());
			        return;
                }
				lastHistoricalBar = Count - 50;
				markUp("Inital_", searchArea, lastHistoricalBar);
				
			}
		}

		protected override void OnOrderUpdate(Cbi.Order order, double limitPrice, double stopPrice, 
			int quantity, int filled, double averageFillPrice, 
			Cbi.OrderState orderState, DateTime time, Cbi.ErrorCode error, string comment)
		{
			
		}

		protected override void OnPositionUpdate(Cbi.Position position, double averagePrice, 
			int quantity, Cbi.MarketPosition marketPosition)
		{
			
		}

		private bool isLocalMinimumTest(int bar) {
			const int side = (searchArea - 1) / 2;
			double lowToTest = Bars.GetLow(bar);
			//Test left
			for (int i = (bar - side); i < bar; i++) {
				double low = Bars.GetLow(i);
				if (low < lowToTest) {
					return false;
				}
			}
			//Test Right
			for (int i = (bar + 1); i < (bar + side + 1); i++) {
				double low = Bars.GetLow(i);
				if (low < lowToTest) {
					return false;
				}
				if (low == lowToTest) {
					return false;
				}
			}
			return true;
		}
		
		private bool isLocalMaximumTest(int bar) {
			const int side = (searchArea - 1) / 2;
			double highToTest =  Bars.GetHigh(bar);
			for (int i = (bar - side); i < bar; i++) {
				double high = Bars.GetHigh(i);
				if (high > highToTest) {
					return false;
				}
			}
			//Test Right
			for (int i = (bar + 1); i < (bar + side + 1); i++) {
				double high = Bars.GetHigh(i);
				if (high > highToTest) {
					return false;
				}
				if (high == highToTest) {
					return false;
				}
			}
			return true;
		}
		
		
		
		
		private Dictionary<int, string> findPivots(int start, int end) {
			Dictionary<int, string> pivots = 
            new Dictionary<int, string>();
			if (Count < searchArea) {
				return pivots;
			}
			
			Log("Start finding Pivots", NinjaTrader.Cbi.LogLevel.Information);
			//i represents middle
			//for (int i = searchArea; i < (Count - (searchArea / 2)); i++) {
			for (int i = start; i < end; i++) {
				bool isLocalMinimum = isLocalMinimumTest(i);
				bool isLocalMaximum = isLocalMaximumTest(i);
				
				if (isLocalMinimum) {
					pivots.Add(i, MIN);
					DateTime timeValue = Bars.GetSessionEndTime(i);
					//Draw.Dot(this, i.ToString() + "Minimim1"  + i.ToString(), true, timeValue, Bars.GetLow(i), Brushes.Blue);
					//Draw.Diamond(this, i.ToString() + "Minimim"  + i.ToString(), true, timeValue, Bars.GetLow(i), Brushes.Blue); 
					
				}
				
				if (isLocalMaximum && !isLocalMinimum) {
					pivots.Add(i, MAX);
					DateTime timeValue = Bars.GetSessionEndTime(i);
					//Draw.Dot(this, i.ToString() + "Maximum1"  + i.ToString(), true, timeValue, Bars.GetHigh(i), Brushes.Blue);
					//DateTime timeValue = Bars.GetSessionEndTime(i);
					//Draw.Diamond(this, i.ToString()  + "Maximum"  + i.ToString(), true, timeValue, Bars.GetHigh(i), Brushes.Orange); 
				}

				//if x period swing
			}
			Log("Pivots Found", NinjaTrader.Cbi.LogLevel.Information);
			return pivots;
		}
		
		
		private Dictionary<int, string> findPeriodSwings(Dictionary<int, string> pivots) {
			Log("Find Period Swings", NinjaTrader.Cbi.LogLevel.Information);
			//Remove non period swings
			List<int> orderedMarkers = pivots.Keys.ToList();
			orderedMarkers.Sort();
			
			Dictionary<int, string> periodSwingsPivots = 
            new Dictionary<int, string>();
			int previousMarker = -1;
			foreach (int marker in orderedMarkers) {
				try {
					if (previousMarker == -1) {
						previousMarker = marker;
						continue;
					}
					
					bool hasSwingPeriod = false;
					if (pivots[previousMarker].Equals(MAX)) {
						//look for x periods where lows are less than close
						for (int start = (previousMarker - SwingPeriod) ; start < (marker+1) ; start++) {
							int end = start + SwingPeriod;
							double minLow = int.MaxValue;
							//Check each swing bar
							for (int swingCount = start ;  swingCount < end ; swingCount++) {
								minLow = Math.Min(minLow, Bars.GetLow(swingCount));
							}
							if (Bars.GetLow(end) < minLow) {
								hasSwingPeriod = true;
								break;
							}
						}
						
					}
				
				
				if (pivots[previousMarker].Equals(MIN)) {
						//look for x periods where highs are more than close
						for (int start = (previousMarker - SwingPeriod) ; start < (marker+1) ; start++) {
							int end = start + SwingPeriod;
							double maxHigh = int.MinValue;
							//Check each swing bar
							for (int swingCount = start ;  swingCount < end ; swingCount++) {
								maxHigh = Math.Max(maxHigh, Bars.GetHigh(swingCount));
							}
							if (Bars.GetHigh(end) > maxHigh) {
								hasSwingPeriod = true;
								break;
							}
						}
						
					}
				
				if (hasSwingPeriod) {
					periodSwingsPivots.Add(previousMarker, pivots[previousMarker]);
				}	
				previousMarker = marker;
				} catch (Exception e) {
					Log(e.ToString(), NinjaTrader.Cbi.LogLevel.Error);
				}
			}
			Log("Period Swings Found", NinjaTrader.Cbi.LogLevel.Information);	
			return periodSwingsPivots;
		}
		
		
		private void plotPivots(Dictionary<int, string> pivots, string prefix) {
			int objnum=0;
			List<int> orderedMarkers = pivots.Keys.ToList();
			orderedMarkers.Sort();
			//orderedMarkers = markers.Sort();
			foreach (int marker in orderedMarkers) {
				if (pivots[marker].Equals(MAX)) {
					DateTime timeValue = Bars.GetSessionEndTime(marker);
					Draw.Dot(this, objnum.ToString() + prefix + "L", false, timeValue, Bars.GetHigh(marker), Brushes.Red); 
					objnum++;
				} else {
					DateTime timeValue = Bars.GetSessionEndTime(marker);
					Draw.Dot(this, objnum.ToString() + prefix + "L", false, timeValue, Bars.GetLow(marker), Brushes.Red); 
					objnum++;
				}
			}	
			
		}
		
		private Dictionary<int, string> removeDuplicatePivots(Dictionary<int, string> periodSwingsPivots) {
			Log("Remove Duplicate Pivots", NinjaTrader.Cbi.LogLevel.Information);
			//Remove duplicate highs / lows
			List<int> orderedMarkers= periodSwingsPivots.Keys.ToList();
			orderedMarkers.Sort();
			int previousMarker = -1;
			string previousMarkerType = "";
			
			Dictionary<int, string> reducedPivots = 
            new Dictionary<int, string>();
			foreach (int marker in orderedMarkers) {
				if (previousMarker == -1) {
					previousMarker = marker;
					previousMarkerType = periodSwingsPivots[previousMarker];
				}
				if (previousMarkerType.Equals(MAX) && periodSwingsPivots[marker].Equals(MAX) && Bars.GetHigh(marker) > Bars.GetHigh(previousMarker)) {
					reducedPivots.Remove(previousMarker);
				}
				if (previousMarkerType.Equals(MIN) && periodSwingsPivots[marker].Equals(MIN) && Bars.GetLow(marker) < Bars.GetLow(previousMarker)) {
					reducedPivots.Remove(previousMarker);
				}
				if (previousMarkerType.Equals(MAX) && periodSwingsPivots[marker].Equals(MAX) && Bars.GetHigh(marker) <= Bars.GetHigh(previousMarker)) {
					continue;
				}
				if (previousMarkerType.Equals(MIN) && periodSwingsPivots[marker].Equals(MIN) && Bars.GetLow(marker) >= Bars.GetLow(previousMarker)) {
					continue;
				}
				
				reducedPivots.Add(marker, periodSwingsPivots[marker]);
				previousMarker = marker;
				previousMarkerType = periodSwingsPivots[previousMarker];
				
			}
			
			Log("Duplicate pivots removed", NinjaTrader.Cbi.LogLevel.Information);	
			return reducedPivots;
		}
		
		
		private void plotSwingPivots(Dictionary<int, string> reducedPivots, string prefix) {
			List<int> orderedMarkers = reducedPivots.Keys.ToList();
			orderedMarkers.Sort();

			int objnum = 0;
			foreach (int marker in orderedMarkers) {
				if (reducedPivots[marker].Equals(MAX)) {
					DateTime timeValue = Bars.GetSessionEndTime(marker);
					Draw.Diamond(this, objnum.ToString() + prefix + "M", false, timeValue, Bars.GetHigh(marker), Brushes.White); 
					objnum++;
				} else {
					DateTime timeValue = Bars.GetSessionEndTime(marker);
					Draw.Diamond(this, objnum.ToString() + prefix + "M", false, timeValue, Bars.GetLow(marker), Brushes.Blue); 
					objnum++;
				}
			}	
		}
		
		
		private void plotSwings(Dictionary<int, string> reducedPivots, string prefix) {
			List<int> orderedMarkers = reducedPivots.Keys.ToList();
			orderedMarkers.Sort();

			int objnum = 0;
			int previousMarker = 0;
			double previousValue=0;
			foreach (int marker in orderedMarkers) {
				double value = 0;
				if (reducedPivots[marker].Equals(MAX)) {
					value = Bars.GetHigh(marker);
				} else {
					value = Bars.GetLow(marker);
				}
				
				if (objnum == 0) {
					previousMarker = marker;
					previousValue = value;
					objnum++;
					continue;	
				}
				DateTime startTimeValue = Bars.GetTime(previousMarker);
				DateTime endTimeValue = Bars.GetTime(marker);
				Draw.Line(this, objnum.ToString() + prefix + "PivotLine", false, startTimeValue, previousValue, endTimeValue, value, Brushes.White,
		                        DashStyleHelper.Dot, 1, true);
				
				objnum++;
				previousValue = value;
				previousMarker = marker;
				
			}	
		}
		
		private void bullishSwing(Dictionary<int, string> reducedPivots, string prefix) {
			
			List<int> orderedMarkers = reducedPivots.Keys.ToList();
			orderedMarkers.Sort();
			int objnum = 0;
			for (int markerIndex = 0; markerIndex < (orderedMarkers.Count()-4); markerIndex++) {
				
				
				int firstIndex = markerIndex;
				int secondIndex = markerIndex + 1;
				int thirdIndex = markerIndex + 2;
				int fourthIndex = markerIndex + 3;
				
				int firstBarIndex = orderedMarkers[firstIndex];
				int secondBarIndex  = orderedMarkers[secondIndex];
				int thirdBarIndex  = orderedMarkers[thirdIndex];
				int fourthBarIndex  = orderedMarkers[fourthIndex];
				
				bool firstValueIsMin = reducedPivots[firstBarIndex].Equals(MIN);
				
				if (!firstValueIsMin) {
					continue;
				}
				
				double firstBarValue = Bars.GetLow(firstBarIndex);
				double secondBarValue  = Bars.GetHigh(secondBarIndex);
				double thirdBarValue  = Bars.GetLow(thirdBarIndex);
				double fourthBarValue  = Bars.GetHigh(fourthBarIndex);
				
				bool firstIsLessThanThird = firstBarValue < thirdBarValue;
				
				if (!firstIsLessThanThird) {
					continue;
				}
				
				bool secondHigher = secondBarValue > firstBarValue && secondBarValue > thirdBarValue;
				
				if (!secondHigher) {
					continue;
				}
				
				bool isSecondTriggered = false;
				
				for (int i = thirdBarIndex ; i < fourthBarIndex; i++) {
					if (Bars.GetHigh(i) > secondBarValue) {
						isSecondTriggered = true;
					}
				}
				
				if (isSecondTriggered) {
					DateTime startTimeValue = Bars.GetTime(firstBarIndex);
					DateTime endTimeValue = Bars.GetTime(secondBarIndex);
					Draw.Line(this, objnum.ToString() + prefix + "Bull", false, startTimeValue, firstBarValue, endTimeValue, secondBarValue, Brushes.Blue,
		                        DashStyleHelper.Solid, 2, true);
					objnum++;
					startTimeValue = Bars.GetTime(secondBarIndex);
					endTimeValue = Bars.GetTime(thirdBarIndex);
					Draw.Line(this, objnum.ToString() +  prefix + "Bull", false, startTimeValue, secondBarValue, endTimeValue, thirdBarValue, Brushes.Blue,
		                        DashStyleHelper.Solid, 2, true);
					objnum++;
				}
				
				
			}
		}
		
		private void bearSwing(Dictionary<int, string> reducedPivots, string prefix) {
			
			List<int> orderedMarkers = reducedPivots.Keys.ToList();
			orderedMarkers.Sort();
			int objnum = 0;
			for (int markerIndex = 0; markerIndex < (orderedMarkers.Count()-4); markerIndex++) {
				
				
				int firstIndex = markerIndex;
				int secondIndex = markerIndex + 1;
				int thirdIndex = markerIndex + 2;
				int fourthIndex = markerIndex + 3;
				
				int firstBarIndex = orderedMarkers[firstIndex];
				int secondBarIndex  = orderedMarkers[secondIndex];
				int thirdBarIndex  = orderedMarkers[thirdIndex];
				int fourthBarIndex  = orderedMarkers[fourthIndex];
				
				bool firstValueIsMax = reducedPivots[firstBarIndex].Equals(MAX);
				
				if (!firstValueIsMax) {
					continue;
				}
				
				double firstBarValue = Bars.GetHigh(firstBarIndex);
				double secondBarValue  = Bars.GetLow(secondBarIndex);
				double thirdBarValue  = Bars.GetHigh(thirdBarIndex);
				double fourthBarValue  = Bars.GetLow(fourthBarIndex);
				
				bool firstIsMoreThanThird = firstBarValue > thirdBarValue;
				
				if (!firstIsMoreThanThird) {
					continue;
				}
				
				bool secondLower = secondBarValue < firstBarValue && secondBarValue < thirdBarValue;
				
				if (!secondLower) {
					continue;
				}
				
				bool isSecondTriggered = false;
				
				for (int i = thirdBarIndex ; i < fourthBarIndex; i++) {
					if (Bars.GetLow(i) < secondBarValue) {
						isSecondTriggered = true;
					}
				}
				
				if (isSecondTriggered) {
					DateTime startTimeValue = Bars.GetTime(firstBarIndex);
					DateTime endTimeValue = Bars.GetTime(secondBarIndex);
					Draw.Line(this, objnum.ToString() +  prefix + "Bear", false, startTimeValue, firstBarValue, endTimeValue, secondBarValue, Brushes.Purple,
		                        DashStyleHelper.Solid, 2, true);
					objnum++;
					startTimeValue = Bars.GetTime(secondBarIndex);
					endTimeValue = Bars.GetTime(thirdBarIndex);
					Draw.Line(this, objnum.ToString() +  prefix +"Bear", false, startTimeValue, secondBarValue, endTimeValue, thirdBarValue, Brushes.Purple,
		                        DashStyleHelper.Solid, 2, true);
					objnum++;
				}
				
				
			}
		}
		
		
		private void markUp(string prefix, int start, int end) {
			//Look at current area mark swing low or swing high
			//look at current area, if pattern possible consider entry
			
			//
			Dictionary<int, string> pivots = findPivots(start, end);
			Dictionary<int, string> periodSwingsPivots = findPeriodSwings(pivots);
			Dictionary<int, string> reducedPivots = removeDuplicatePivots(periodSwingsPivots);
			plotPivots(pivots, prefix);
			plotSwingPivots(reducedPivots, prefix);
			plotSwings(reducedPivots, prefix);
			bullishSwing(reducedPivots, prefix);
			bearSwing(reducedPivots, prefix);
			
			
			
			
			/*
			//Start at 3
			//identify pattern
			//itentify trigger
			orderedMarkers = reducedPivots.Keys.ToList();
			orderedMarkers.Sort();
			int markerCount = 0;
			for (int markerCount = 0 ; markerCount < reducedPivots.Count() ; markerCount++) {
				if (markerCount < 4) {
					continue;
				}
				isBullishSwing;
				o
				isBearishSwing;
				
			}*/
		}
		
		
		
		
		protected override void OnBarUpdate()
		{
			//markUp();
			markUp("Real_", lastHistoricalBar -20, Count);
			
		}		[NinjaScriptProperty]
		[Range(3, int.MaxValue)]
		[Display(Name="SwingPeriod", Description="Swing Period 3, 5, 7", Order=1, GroupName="Parameters")]
		public int SwingPeriod
		{ get; set; }





	}
}
