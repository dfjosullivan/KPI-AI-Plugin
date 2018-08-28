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
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;

#endregion

//This namespace holds Add ons in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.AddOns
{
	public class DisplaySwingChange : NinjaTrader.NinjaScript.AddOnBase
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Add on here.";
				Name										= "DisplaySwingChange";
				DataToLoad					= string.Empty;
                
            }
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnWindowCreated(Window window)
		{
		    
        }


		#region Properties
		[NinjaScriptProperty]
		[Display(Name="DataToLoad", Description="DataToLoad", Order=1, GroupName="Parameters")]
		public string DataToLoad
		{ get; set; }
		#endregion

	}
}


namespace NinjaTrader.NinjaScript.Indicators
{
    public class MyFirstIndicator : Indicator
    {
        #region Indicator Name / Description

        private string iProductVersion = " v1.0.1";
        private string iProductName = "My First Indicator";
        private string iProductDescrption = string.Empty;

        public override string DisplayName { get { return string.Format("{0} {1}", iProductName, iProductVersion); } }

        #endregion

        private double iOffset = 0;
        private int iOffsetTicks = 1;
        [NinjaScriptProperty]
        [Display(Name = "01. Text Offset (ticks)", Description = "Description of input goes here", GroupName = "01. Indicator Parameters", Order = 1)]
        public int OffsetTicks
        {
            get { return iOffsetTicks; }
            set { iOffsetTicks = Math.Max(0, value); }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                // Name / Description
                Name = string.Format("{0} {1}", iProductName, iProductVersion);
                Description = iProductDescrption;
                // Visuals
                IsOverlay = true;
                IsAutoScale = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                BarsRequiredToPlot = 1;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ShowTransparentPlotsInDataBox = false;
                ScaleJustification = ScaleJustification.Right;

                // Misc
                IsChartOnly = false;
                Calculate = Calculate.OnBarClose;
                IsSuspendedWhileInactive = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
            }
            else if (State == State.DataLoaded)
            {
                iOffset = iOffsetTicks * TickSize;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 0)
                return;

            if (IsFirstTickOfBar)
            {
                if (CurrentBar == (Count -2))
                {
                    System.Windows.Forms.MessageBox.Show("My message here");
                }

                DateTime timeValue = Bars.GetTime(CurrentBar);
                string xCB = CurrentBar.ToString();
                if (CurrentBar % 2 == 0)
                    Draw.Text(this, "Even " + xCB, xCB, 0, High[0] + iOffset);
                else
                    Draw.Text(this, "Odd " + xCB, xCB, 0, Low[0] - iOffset);
            }
        }
    }
}