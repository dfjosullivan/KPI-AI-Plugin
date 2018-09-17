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
		private const string MIN = "minimum";
		private const string MAX = "maximum";
		private const string MIN_MAX = "minimum_maximum";
		private int lastHistoricalBar;
		private int lastBullTrigger =0;
		private int lastBearTrigger =0;
		
		
		private string OBSERVE_BULL = "bull";
		private string OBSERVE_BEAR = "bear";
		private double observeTrigger;
		private double triggerRisk = 0;
		private string observePattern;
		private int lastPatternIndex=0;
		private string WAITING_FOR_TRIGGER = "waitingForTrigger";
		private string TRIGGER_EXECUTED = "triggerExecuted";
		private string triggerStatus = "UNKNOWN";
		private int lastProcessedBar =0;
		
		private bool notConfigured = true;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Swing Change Pattern v1.0.5";
				Calculate									= Calculate.OnPriceChange;
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
				AutoTrade 					= false;
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
					notConfigured=true;
					return;
				} 
				if (!allowedInstruments.Contains(Instrument.FullName)) {
					MessageBox.Show("Only YM or YM 09-18 or YM- 12-18 allowed, not " + Instrument.FullName);
					notConfigured=true;
					return;
				}

			    if (!Bars.BarsPeriod.MarketDataType.Equals(MarketDataType.Last))
			    {
			        MessageBox.Show("Last data only not " + Bars.BarsPeriod.MarketDataType.ToString());
			        notConfigured=true;
					return;
                }
				lastHistoricalBar = Count - 50;
				markUp("Inital_", searchArea, lastHistoricalBar);
				notConfigured=false;
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

		private bool isSwingLowTest(int bar) {
			double thisBarLow = Bars.GetLow(bar);
			bool nextLowBarHigher = Bars.GetLow(bar + 1) >= thisBarLow;
			//Check enough previous bars
			if (!nextLowBarHigher) {
			//	return false;
			}
			
			// check previous X bars higher than this bar's low
			double lowestPreviousPeriod = Bars.GetHigh(bar);
			int previousBarIndex = bar - 1;
			int lastBarToCheckIndex = bar - SwingPeriod;
			for (int i = previousBarIndex; i >= lastBarToCheckIndex; i--) {
				//bool doubleBottom = (i == previousBarIndex) && Bars.GetLow(i).Equals(thisBarLow);
				//if (!doubleBottom) {
				if (isInsideBar(i)) {
					lastBarToCheckIndex--;
				} else {
					lowestPreviousPeriod = Math.Min(lowestPreviousPeriod, Bars.GetLow(i));
				}
			//	}
			}
			
			return thisBarLow < lowestPreviousPeriod;
		}
		
		private bool isSwingHighTest(int bar) {
			double thisBarHigh = Bars.GetHigh(bar);
			bool nextHighBarLower= Bars.GetHigh(bar + 1) <= thisBarHigh;
			//Check enough previous bars
			if (!nextHighBarLower) {
			//	return false;
			}
			
			// check previous X bars higher than this bar's low
			double highestPreviousPeriod = Bars.GetLow(bar);
			int previousBarIndex = bar - 1;
			int lastBarToCheckIndex = bar - SwingPeriod;
			for (int i = previousBarIndex; i >= lastBarToCheckIndex; i--) {
			//	bool doubleTop = (i == previousBarIndex) && Bars.GetHigh(i).Equals(thisBarHigh);
			//	if (!doubleTop) {
				if (isInsideBar(i)) {
					lastBarToCheckIndex--;
				} else {
					highestPreviousPeriod = Math.Max(highestPreviousPeriod, Bars.GetHigh(i));
				}
			//	}
			}
			
			return thisBarHigh > highestPreviousPeriod;
		}
		
		
		private bool isInsideBar(int barIndex) {
			int previousBarIndex = barIndex -1;			
			bool withinHigh = Bars.GetHigh(barIndex) <= Bars.GetHigh(previousBarIndex);
			bool withinLow = Bars.GetLow(barIndex) >= Bars.GetLow(previousBarIndex);
			return withinHigh && withinLow;
		}
		
		
		private Dictionary<int, string> findPeriodSwings(int start, int end) {
			Log("Find Period Swings", NinjaTrader.Cbi.LogLevel.Information);
			//Remove non period swings
			
			Dictionary<int, string> periodSwings = 
            new Dictionary<int, string>();

			for (int i = start; i < end; i++) {
				try {
					bool isSwingLow  = isSwingLowTest(i);
					bool isSwingHigh = isSwingHighTest(i);
					
					if (isSwingLow && isSwingHigh) {
						periodSwings.Add(i, MIN_MAX);
					}
					
					if (isSwingLow) {
						periodSwings.Add(i, MIN);
					}
					
					if (isSwingHigh) {
						periodSwings.Add(i, MAX);
					}
					
					

				} catch (Exception e) {
					Log(e.ToString(), NinjaTrader.Cbi.LogLevel.Error);
				}
			}
			Log("Period Swings Found", NinjaTrader.Cbi.LogLevel.Information);	
			return periodSwings;
		}
		
		
		private void plotPivots(Dictionary<int, string> pivots, string prefix) {
			int objnum=0;
			List<int> orderedMarkers = pivots.Keys.ToList();
			orderedMarkers.Sort();
			//orderedMarkers = markers.Sort();
			foreach (int marker in orderedMarkers) {
				if (pivots[marker].Equals(MIN_MAX)) {
					DateTime timeValue = Bars.GetSessionEndTime(marker);
					Draw.Dot(this, objnum.ToString() + prefix + "L", false, timeValue, Bars.GetHigh(marker), Brushes.Red);
					objnum++;
					Draw.Dot(this, objnum.ToString() + prefix + "L", false, timeValue, Bars.GetLow(marker), Brushes.Red);
					objnum++;
				} else if (pivots[marker].Equals(MAX)) {
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
			string previous = "";
			foreach (int marker in orderedMarkers) {
				if (reducedPivots[marker].Equals(MIN_MAX) && previous.Equals(MAX)) {
					DateTime timeValue = Bars.GetSessionEndTime(marker);
					Draw.Diamond(this, objnum.ToString() + prefix + "M", false, timeValue, Bars.GetLow(marker), Brushes.Blue); 
					objnum++;
					timeValue = Bars.GetSessionEndTime(marker);
					Draw.Diamond(this, objnum.ToString() + prefix + "M", false, timeValue, Bars.GetHigh(marker), Brushes.White); 
					objnum++;
				} else if (reducedPivots[marker].Equals(MIN_MAX) && previous.Equals(MIN)) {
					DateTime timeValue = Bars.GetSessionEndTime(marker);
					Draw.Diamond(this, objnum.ToString() + prefix + "M", false, timeValue, Bars.GetHigh(marker), Brushes.White); 
					objnum++;
					timeValue = Bars.GetSessionEndTime(marker);
					Draw.Diamond(this, objnum.ToString() + prefix + "M", false, timeValue, Bars.GetLow(marker), Brushes.Blue); 
					objnum++;
				} else if (reducedPivots[marker].Equals(MAX)) {
					DateTime timeValue = Bars.GetSessionEndTime(marker);
					Draw.Diamond(this, objnum.ToString() + prefix + "M", false, timeValue, Bars.GetHigh(marker), Brushes.White); 
					objnum++;
				} else {
					DateTime timeValue = Bars.GetSessionEndTime(marker);
					Draw.Diamond(this, objnum.ToString() + prefix + "M", false, timeValue, Bars.GetLow(marker), Brushes.Blue); 
					objnum++;
				}
				previous = reducedPivots[marker];
			}	
		}
		
		
		private void plotSwings(Dictionary<int, string> reducedPivots, string prefix) {
			List<int> orderedMarkers = reducedPivots.Keys.ToList();
			orderedMarkers.Sort();

			int objnum = 0;
			int previousMarker = 0;
			double previousValue=0;
			string previous = "";
			foreach (int marker in orderedMarkers) {
				double value = 0;
				if (reducedPivots[marker].Equals(MIN_MAX) && previous.Equals(MIN)) {
					value = Bars.GetHigh(marker);
				} else if (reducedPivots[marker].Equals(MIN_MAX) && previous.Equals(MAX)) {
					value = Bars.GetLow(marker);
				} else if (reducedPivots[marker].Equals(MAX)) {
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
				previous = reducedPivots[marker];
				
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
		
		
		void activateBearSwing(int firstIndex, int secondIndex, int thirdIndex) {
			//Take last 3
			
			//Check if bear swing
			double firstBarValue = Bars.GetHigh(firstIndex);
			double secondBarValue  = Bars.GetLow(secondIndex);
			double thirdBarValue  = Bars.GetHigh(thirdIndex);
			
			bool firstIsMoreThanThird = firstBarValue > thirdBarValue;
				
			if (!firstIsMoreThanThird) {
				return;
			}
				
			bool secondLower = secondBarValue < firstBarValue && secondBarValue < thirdBarValue;
				
			if (!secondLower) {
				return;
			}
			
			if (thirdIndex > lastPatternIndex) {
				observePattern = OBSERVE_BEAR;
				observeTrigger = secondBarValue;
				lastPatternIndex = thirdIndex;
			 	triggerStatus = WAITING_FOR_TRIGGER;
				triggerRisk = (thirdBarValue - secondBarValue) * 1.2;
				
				
				DateTime startTimeValue = Bars.GetTime(firstIndex);
				DateTime endTimeValue = Bars.GetTime(secondIndex);
				Draw.Line(this, "ActiveSwing1", false, startTimeValue, firstBarValue, endTimeValue, secondBarValue, Brushes.Yellow,
			                        DashStyleHelper.Solid, 2, true);
				startTimeValue = Bars.GetTime(secondIndex);
				endTimeValue = Bars.GetTime(thirdIndex);
				Draw.Line(this, "ActiveSwing2", false, startTimeValue, secondBarValue, endTimeValue, thirdBarValue, Brushes.Yellow,
			                        DashStyleHelper.Solid, 2, true);
			}
			
		}
		
		void activateBullSwing(int firstIndex, int secondIndex, int thirdIndex) {
			//Take last 3
			
			//Check if bull swing
			double firstBarValue = Bars.GetLow(firstIndex);
			double secondBarValue  = Bars.GetHigh(secondIndex);
			double thirdBarValue  = Bars.GetLow(thirdIndex);
			
			bool firstIsLessThanThird = firstBarValue < thirdBarValue;
				
			if (!firstIsLessThanThird) {
				return;
			}
				
			bool secondHigher = secondBarValue > firstBarValue && secondBarValue > thirdBarValue;
				
			if (!secondHigher) {
				return;
			}
			
			if (thirdIndex > lastPatternIndex) {
				observePattern = OBSERVE_BULL;
				observeTrigger = secondBarValue;
				lastPatternIndex = thirdIndex;
				triggerStatus = WAITING_FOR_TRIGGER;
				triggerRisk = (secondBarValue - thirdBarValue) * 1.2;
				
				DateTime startTimeValue = Bars.GetTime(firstIndex);
				DateTime endTimeValue = Bars.GetTime(secondIndex);
				Draw.Line(this, "ActiveSwing1", false, startTimeValue, firstBarValue, endTimeValue, secondBarValue, Brushes.Yellow,
			                        DashStyleHelper.Solid, 2, true);
				startTimeValue = Bars.GetTime(secondIndex);
				endTimeValue = Bars.GetTime(thirdIndex);
				Draw.Line(this, "ActiveSwing2", false, startTimeValue, secondBarValue, endTimeValue, thirdBarValue, Brushes.Yellow,
			                        DashStyleHelper.Solid, 2, true);
			}
			
		}
		
		private Dictionary<int, string> markUp(string prefix, int start, int end) {
			//Look at current area mark swing low or swing high
			//look at current area, if pattern possible consider entry
			
			//
			Dictionary<int, string> periodSwingsPivots = findPeriodSwings(start, end);
			Dictionary<int, string> reducedPivots = removeDuplicatePivots(periodSwingsPivots);
			//plotPivots(periodSwingsPivots, prefix);
		    plotSwingPivots(reducedPivots, prefix);
			plotSwings(reducedPivots, prefix);
			bullishSwing(reducedPivots, prefix);
			bearSwing(reducedPivots, prefix);
			
			return reducedPivots;
			
			//take last 3
			
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
			if (CurrentBar < (Count-2)) {
				return;
			}
				
			if (notConfigured) {
				return;	
			}
			
			bool isNewBar = CurrentBar > lastProcessedBar;
			if (IsFirstTickOfBar) {
				lastProcessedBar = CurrentBar;
				Dictionary<int, string> reducedPivots = markUp("Real_", lastHistoricalBar, CurrentBar);
				
				int countPivots = reducedPivots.Count();
				if (countPivots < 4) {
					return;
				}
				int i =0;
				

			    List<int> orderedMarkers = reducedPivots.Keys.ToList();
			    orderedMarkers.Sort();
				for (i=0; i< Math.Min(5,countPivots - 3);i++) {
				    int firstIndex = orderedMarkers[countPivots - 4 - i];
				    int secondIndex = orderedMarkers[countPivots - 3 - i];
		            int thirdIndex = orderedMarkers[countPivots - 2 - i];


		            //Remove all not min max
		            bool hasMinMax = reducedPivots[firstIndex].Equals(MIN_MAX) || reducedPivots[secondIndex].Equals(MIN_MAX) || reducedPivots[thirdIndex].Equals(MIN_MAX);
					
					
					//bool possibleBear = reducedPivots[firstIndex].Equals(MAX) || reducedPivots[secondIndex].Equals(MIN) || reducedPivots[thirdIndex].Equals(MAX);
					//bool possibleBull = reducedPivots[firstIndex].Equals(MIN) || reducedPivots[secondIndex].Equals(MAX) || reducedPivots[thirdIndex].Equals(MIN);
					
					bool possibleBear = (reducedPivots[secondIndex].Equals(MIN_MAX) && reducedPivots[thirdIndex].Equals(MIN)) ||
						(reducedPivots[firstIndex].Equals(MAX) && reducedPivots[secondIndex].Equals(MIN) && reducedPivots[thirdIndex].Equals(MAX))||
						(reducedPivots[secondIndex].Equals(MAX) && reducedPivots[thirdIndex].Equals(MIN_MAX));
					bool possibleBull = (reducedPivots[secondIndex].Equals(MIN_MAX) && reducedPivots[thirdIndex].Equals(MAX)) ||
						(reducedPivots[firstIndex].Equals(MIN) && reducedPivots[secondIndex].Equals(MAX) &&  reducedPivots[thirdIndex].Equals(MIN)) ||
						(reducedPivots[secondIndex].Equals(MIN) && reducedPivots[thirdIndex].Equals(MIN_MAX));
					
					if (reducedPivots[thirdIndex].Equals(MIN_MAX)) {
						firstIndex = secondIndex;
						secondIndex = thirdIndex;
					} else  if (reducedPivots[secondIndex].Equals(MIN_MAX)){
						firstIndex = secondIndex;
					}
					
					if (possibleBear) {
						activateBearSwing(firstIndex, secondIndex, thirdIndex);
					}
					
					if (possibleBull) {
						activateBullSwing(firstIndex, secondIndex, thirdIndex);
					}
				}
			}
			
			if (triggerStatus.Equals(WAITING_FOR_TRIGGER) && AutoTrade && observePattern.Equals(OBSERVE_BULL) && Close[0] > observeTrigger) {
				Log("Triggered Bull Current Value: " + Close[0].ToString() + " Trigger Value: " + observeTrigger.ToString(), LogLevel.Information);
				ExitShort();
				EnterLong(1);
				//SetProfitTarget(CalculationMode.Ticks, triggerRisk);
				//SetStopLoss(CalculationMode.Ticks, triggerRisk);
				SetTrailStop(CalculationMode.Ticks, triggerRisk);
				triggerStatus = TRIGGER_EXECUTED;
			}
			if (triggerStatus.Equals(WAITING_FOR_TRIGGER) && AutoTrade && observePattern.Equals(OBSERVE_BEAR) && Close[0] < observeTrigger) {
				Log("Triggered Bear Current Value: " + Close[0].ToString() + " Trigger Value: " + observeTrigger.ToString(), LogLevel.Information);
				ExitLong();
				EnterShort(1);
				//SetProfitTarget(CalculationMode.Ticks, triggerRisk);
				//SetStopLoss(CalculationMode.Ticks, triggerRisk);
				SetTrailStop(CalculationMode.Ticks, triggerRisk);	
				triggerStatus = TRIGGER_EXECUTED;
			}
			
		}		[NinjaScriptProperty]
		[Range(3, int.MaxValue)]
		[Display(Name="SwingPeriod", Description="Swing Period 3, 5, 7", Order=1, GroupName="Parameters")]
		public int SwingPeriod
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="Auto Trade", Description="Automatically Trade", Order=3, GroupName="Parameters")]
		public bool AutoTrade
		{ get; set; }




	}
}
