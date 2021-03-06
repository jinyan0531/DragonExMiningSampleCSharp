﻿using DragonExMiningSampleCSharp.Apis.Common;
using DragonExMiningSampleCSharp.Apis.Config;
using DragonExMiningSampleCSharp.Apis.DragonEx;
using DragonExMiningSampleCSharp.Apis.Entity;
using DragonExMiningSampleCSharp.Apis.Log;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DragonExMiningSampleCSharp.Apis.Common.Enums;

namespace DragonExMiningSampleCSharp
{
    public partial class DragonExMiningTool : Form
    {
        /// <summary>
        /// Api For A Account
        /// </summary>
        private DragonExApiImpl dragonExApiForA = new DragonExApiImpl(DragonExConstants.SECRET_KEY_A, DragonExConstants.ACCESS_KEY_A);

        /// <summary>
        /// Api For B Account
        /// </summary>
        private DragonExApiImpl dragonExApiForB = new DragonExApiImpl(DragonExConstants.SECRET_KEY_B, DragonExConstants.ACCESS_KEY_B);

        /// <summary>
        /// Thread for get market info
        /// </summary>
        private Thread getMarketInfoThread;

        /// <summary>
        /// Thread for get user info
        /// </summary>
        private Thread getUserInfoThread;

        /// <summary>
        /// Trading thread
        /// </summary>
        private Thread tradingThread;

        /// <summary>
        /// Trading entity
        /// </summary>
        private TradingEntity tradingEntity;

        /// <summary>
        /// Whether auto trading
        /// </summary>
        private bool autoTrading = false;

        /// <summary>
        /// Whether auto generate mine price
        /// </summary>
        private bool autoGenerateMinePrice = false;

        /// <summary>
        /// Mine price
        /// </summary>
        private decimal minePrice = 0.0000000001m;

        /// <summary>
        /// Previous mine price
        /// </summary>
        private decimal previousMinePrice = 0.0000000001m;

        /// <summary>
        /// Whether mine amount is unlimited
        /// </summary>
        private bool mineAmountUnlimited = false;

        /// <summary>
        /// Mine amount
        /// </summary>
        private decimal mineAmount = 0.0000000001m;

        /// <summary>
        /// Previous mine amount
        /// </summary>
        private decimal previousMineAmount = 0.0000000001m;

        /// <summary>
        /// Wheter trade interval is unlimited
        /// </summary>
        private bool tradeIntervalUnlimited = false;

        /// <summary>
        /// Trade interval
        /// </summary>
        private int tradeInterval = 10;

        /// <summary>
        /// Trade side
        /// </summary>
        private TradeSide tradeSide = TradeSide.BOTH_A_B_A_FIRST;

        /// <summary>
        /// Trade method
        /// </summary>
        private TradeMethod tradeMethod = TradeMethod.A_TO_B;

        /// <summary>
        /// Last trade time
        /// </summary>
        private long lastTradeTime = 0;

        /// <summary>
        /// Whether pair is changed
        /// </summary>
        private bool pairChanged = true;

        /// <summary>
        /// Reset trade depth
        /// </summary>
        /// <param name="tde"></param>
        private delegate void ResetTradeDepth(TradeDepthEntity tde);

        /// <summary>
        /// Reset UI
        /// </summary>
        private delegate void ResetUI();

        /// <summary>
        /// Profits entity
        /// </summary>
        private ProfitsEntity profits = new ProfitsEntity();

        public DragonExMiningTool()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Execute get user info
        /// </summary>
        public void ExecGetUserInfos()
        {
            while (true)
            {
                GetUserInfo();
                Thread.Sleep(ConfigTool.UserInfoGetInterval);
            }
        }

        /// <summary>
        /// Get user info
        /// </summary>
        public void GetUserInfo()
        {
            try
            {
                dragonExApiForA.UpdateUserInfo(pairChanged);
                dragonExApiForB.UpdateUserInfo(pairChanged);
                pairChanged = false;
                this.Invoke(new ResetUI(ResetCurrentUserInfos), new object[] { });
            }
            catch (ThreadAbortException)
            {
                //Do nothing when aborting thread
            }
            catch (Exception ex)
            {
                //Error happened when getting
                var logMsg = "Exception happened when getting User Infos.Details:" + Environment.NewLine
                    + ex.Message + Environment.NewLine + ex.StackTrace;
                Console.WriteLine(logMsg);
                LogTool.LogTradeInfo(logMsg, LogLevels.ERROR);
            }
        }

        /// <summary>
        /// Reset current user infos
        /// </summary>
        private void ResetCurrentUserInfos()
        {
            if (dragonExApiForA.UserAmounts != null)
            {
                aBaseLbl.Text = dragonExApiForA.UserAmounts.BaseAmount.ToString();
                aCoinLbl.Text = dragonExApiForA.UserAmounts.CoinAmount.ToString();
                aUpdatedLbl.Text = dragonExApiForA.UserAmounts.UpdatedDate.ToString("yyyy/MM/dd HH:mm:ss");
            }

            if (dragonExApiForB.UserAmounts != null)
            {
                bBaseLbl.Text = dragonExApiForB.UserAmounts.BaseAmount.ToString();
                bCoinLbl.Text = dragonExApiForB.UserAmounts.CoinAmount.ToString();
                bUpdatedLbl.Text = dragonExApiForB.UserAmounts.UpdatedDate.ToString("yyyy/MM/dd HH:mm:ss");
            }

            if (dragonExApiForA.UserAmounts != null
                && dragonExApiForA.UserAmounts != null)
            {
                profits.UpdateCurrent(dragonExApiForA.UserAmounts, dragonExApiForB.UserAmounts);
                baseBaseLbl.Text = profits.BaseBaseAmount.ToString();
                baseCoinLbl.Text = profits.BaseCoinAmount.ToString();
                currentBaseLbl.Text = profits.CurrentBaseAmount.ToString();
                currentCoinLbl.Text = profits.CurrentCoinAmount.ToString();
                profitsBaseLbl.Text = profits.ProfitsBaseAmount.ToString();
                profitsCoinLbl.Text = profits.ProfitsCoinAmount.ToString();
            }
        }

        /// <summary>
        /// Execute get market info
        /// </summary>
        public void ExecGetMarketInfo()
        {
            while (true)
            {
                try
                {
                    dragonExApiForA.UpdateTradeDepth(ConfigTool.CurrentPair.Value);
                    this.Invoke(new ResetTradeDepth(ResetCurrentDepth), new object[] { dragonExApiForA.Tde });
                    this.Invoke(new ResetUI(UpdateCurrentUI), new object[] { });
                }
                catch (ThreadAbortException)
                {
                    //Do nothing when aborting thread
                }
                catch (Exception ex)
                {
                    //Error happened when getting
                    var logMsg = "Exception happened when getting Ticker.Details:" + Environment.NewLine
                        + ex.Message + Environment.NewLine + ex.StackTrace;
                    Console.WriteLine(logMsg);
                    LogTool.LogTradeInfo(logMsg, LogLevels.ERROR);
                }
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Reset current depth
        /// </summary>
        /// <param name="tde">Trade depth entity</param>
        private void ResetCurrentDepth(TradeDepthEntity tde)
        {
            if (tde == null)
            {
                return;
            }
            AskGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            AskGrid.ReadOnly = true;
            BidGrid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            BidGrid.ReadOnly = true;
            AskGrid.DataSource = tde.AsksList;
            AskGrid.AutoResizeColumns();
            BidGrid.DataSource = tde.BidsList;
            BidGrid.AutoResizeColumns();
        }

        /// <summary>
        /// Update Current UI
        /// </summary>
        private void UpdateCurrentUI()
        {
            if (dragonExApiForA.Tde == null)
            {
                return;
            }
            // If auto generate mine price
            if (autoGenerateMinePrice)
            {
                minePrice = (dragonExApiForA.Tde.MaxBid.Price + dragonExApiForA.Tde.MinAsk.Price) / 2;
                minePriceNud.Value = minePrice;
            }
            if (autoTrading)
            {
                ExecTrading();
            }
        }

        /// <summary>
        /// Execute trading
        /// </summary>
        private void ExecTrading()
        {
            switch (tradeSide)
            {
                case TradeSide.A_TO_B:
                    // If not trading now
                    if (tradingThread == null || !tradingThread.IsAlive)
                    {
                        // If mine amount is unlimited
                        if (mineAmountUnlimited)
                        {
                            var aMaxAmount = dragonExApiForA.UserAmounts.CoinAmount;
                            var bMaxAmount = dragonExApiForB.UserAmounts.BaseAmount / (minePrice * (1 + ConfigTool.TradeFee));
                            mineAmount = Math.Min(aMaxAmount, bMaxAmount);
                        }
                        if (mineAmount * minePrice > ConfigTool.MinimumTradeUsdtAmount)
                        {
                            if (mineAmountUnlimited ||
                                (mineAmount <= dragonExApiForA.UserAmounts.CoinAmount
                                && mineAmount * minePrice * (1 + ConfigTool.TradeFee) <= dragonExApiForB.UserAmounts.BaseAmount))
                            {
                                // Do trading
                                long currentTS = DateTime.Now.Ticks;
                                if (tradeIntervalUnlimited || currentTS - lastTradeTime >= tradeInterval * 10000000)
                                {
                                    lastTradeTime = currentTS;
                                    logTxt.Clear();
                                    tradingEntity = new TradingEntity();
                                    tradingEntity.Buy = minePrice;
                                    tradingEntity.Sell = minePrice;
                                    tradingEntity.Amount = mineAmount;
                                    tradingThread = new Thread(ExecTradingAB);
                                    tradingThread.Start();
                                }
                            }
                            else
                            {
                                if (!mineAmountUnlimited)
                                {
                                    logTxt.Text = "";
                                    ResetTradingLog("Trading A=>B is ended.No Coin or Base is available.", true);
                                }
                            }
                        }
                        else
                        {
                            logTxt.Text = "";
                            ResetTradingLog("Trading A=>B is ended.No Coin or Base is available.", true);
                        }
                    }
                    break;
                case TradeSide.B_TO_A:
                    // If not trading now
                    if (tradingThread == null || !tradingThread.IsAlive)
                    {
                        // If mine amount is unlimited
                        if (mineAmountUnlimited)
                        {
                            var aMaxAmount = dragonExApiForB.UserAmounts.CoinAmount;
                            var bMaxAmount = dragonExApiForA.UserAmounts.BaseAmount / (minePrice * (1 + ConfigTool.TradeFee));
                            mineAmount = Math.Min(aMaxAmount, bMaxAmount);
                        }
                        if (mineAmount * minePrice > ConfigTool.MinimumTradeUsdtAmount)
                        {
                            if (mineAmountUnlimited ||
                                (mineAmount <= dragonExApiForB.UserAmounts.CoinAmount
                                && mineAmount * minePrice * (1 + ConfigTool.TradeFee) <= dragonExApiForA.UserAmounts.BaseAmount))
                            {
                                // Do trading
                                long currentTS = DateTime.Now.Ticks;
                                if (tradeIntervalUnlimited || currentTS - lastTradeTime >= tradeInterval * 10000000)
                                {
                                    lastTradeTime = currentTS;
                                    logTxt.Clear();
                                    tradingEntity = new TradingEntity();
                                    tradingEntity.Buy = minePrice;
                                    tradingEntity.Sell = minePrice;
                                    tradingEntity.Amount = mineAmount;
                                    tradingThread = new Thread(ExecTradingBA);
                                    tradingThread.Start();
                                }
                            }
                            else
                            {
                                if (!mineAmountUnlimited)
                                {
                                    logTxt.Text = "";
                                    ResetTradingLog("Trading B=>A is ended.No Coin or Base is available.", true);
                                }
                            }
                        }
                        else
                        {
                            logTxt.Text = "";
                            ResetTradingLog("Trading B=>A is ended.No Coin or Base is available.", true);
                        }
                    }
                    break;
                case TradeSide.BOTH_A_B_A_FIRST:
                case TradeSide.BOTH_A_B_B_FIRST:
                    // If not trading now
                    if (tradingThread == null || !tradingThread.IsAlive)
                    {
                        // If mine amount is unlimited
                        if (mineAmountUnlimited)
                        {
                            var aMaxAmount = dragonExApiForA.UserAmounts.CoinAmount;
                            var bMaxAmount = dragonExApiForB.UserAmounts.BaseAmount / (minePrice * (1 + ConfigTool.TradeFee));
                            var aToBMineAmount = Math.Min(aMaxAmount, bMaxAmount);

                            aMaxAmount = dragonExApiForB.UserAmounts.CoinAmount;
                            bMaxAmount = dragonExApiForA.UserAmounts.BaseAmount / (minePrice * (1 + ConfigTool.TradeFee));
                            var bToAMineAmount = Math.Min(aMaxAmount, bMaxAmount);

                            if (aToBMineAmount * minePrice <= ConfigTool.MinimumTradeUsdtAmount
                                && bToAMineAmount * minePrice <= ConfigTool.MinimumTradeUsdtAmount)
                            {
                                logTxt.Text = "";
                                ResetTradingLog("Trade is ended.Trade amount and price is under minimum.", true);
                                return;
                            }

                            switch (tradeMethod)
                            {
                                case TradeMethod.A_TO_B:
                                    mineAmount = aToBMineAmount;
                                    break;
                                case TradeMethod.B_TO_A:
                                    mineAmount = bToAMineAmount;
                                    break;
                            }
                        }
                        if (mineAmount * minePrice > ConfigTool.MinimumTradeUsdtAmount)
                        {
                            if (mineAmountUnlimited ||
                                (tradeMethod == TradeMethod.A_TO_B && mineAmount <= dragonExApiForA.UserAmounts.CoinAmount
                                && mineAmount * minePrice * (1 + ConfigTool.TradeFee) <= dragonExApiForB.UserAmounts.BaseAmount) ||
                                (tradeMethod == TradeMethod.B_TO_A && mineAmount <= dragonExApiForB.UserAmounts.CoinAmount
                                && mineAmount * minePrice * (1 + ConfigTool.TradeFee) <= dragonExApiForA.UserAmounts.BaseAmount))
                            {
                                // Do trading
                                long currentTS = DateTime.Now.Ticks;
                                if (tradeIntervalUnlimited || currentTS - lastTradeTime >= tradeInterval * 10000000)
                                {
                                    lastTradeTime = currentTS;
                                    logTxt.Clear();
                                    lastTradeTime = currentTS;
                                    tradingEntity = new TradingEntity();
                                    tradingEntity.Buy = minePrice;
                                    tradingEntity.Sell = minePrice;
                                    tradingEntity.Amount = mineAmount;
                                    switch (tradeMethod)
                                    {
                                        case TradeMethod.A_TO_B:
                                            tradingThread = new Thread(ExecTradingAB);
                                            break;
                                        case TradeMethod.B_TO_A:
                                            tradingThread = new Thread(ExecTradingBA);
                                            break;
                                    }
                                    tradingThread.Start();
                                }
                            }
                            else
                            {
                                if (!mineAmountUnlimited)
                                {
                                    // If mine amount is not unlimited, then go to another side
                                    switch (tradeMethod)
                                    {
                                        case TradeMethod.A_TO_B:
                                            tradeMethod = TradeMethod.B_TO_A;
                                            break;
                                        case TradeMethod.B_TO_A:
                                            tradeMethod = TradeMethod.A_TO_B;
                                            break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (mineAmountUnlimited)
                            {
                                // If mine amount is unlimited, then go to another side
                                switch (tradeMethod)
                                {
                                    case TradeMethod.A_TO_B:
                                        tradeMethod = TradeMethod.B_TO_A;
                                        break;
                                    case TradeMethod.B_TO_A:
                                        tradeMethod = TradeMethod.A_TO_B;
                                        break;
                                }
                            }
                            else
                            {
                                logTxt.Text = "";
                                ResetTradingLog("Trade is ended.Trade amount and price is under minimum.", true);
                            }
                        }
                    }
                    break;
            }
        }

        private void DragonExMiningTool_Load(object sender, EventArgs e)
        {
            var pairList = new List<string>();
            foreach(var item in ConfigTool.TradePairDict)
            {
                pairList.Add(item.Key);
            }
            pairCmb.DataSource = pairList;
            aAccBaseLbl.Text = ConfigTool.CurrentBase.ToUpper() + ":";
            aAccCoinLbl.Text = ConfigTool.CurrentCoin.ToUpper() + ":";
            bAccBaseLbl.Text = ConfigTool.CurrentBase.ToUpper() + ":";
            bAccCoinLbl.Text = ConfigTool.CurrentCoin.ToUpper() + ":";
            profitsAllBaseLbl.Text = "All " + ConfigTool.CurrentBase.ToUpper() + ":";
            profitsAllCoinLbl.Text = "All " + ConfigTool.CurrentCoin.ToUpper() + ":";
            autoTradeChk.Checked = false;

            if (getMarketInfoThread == null || !getMarketInfoThread.IsAlive)
            {
                getMarketInfoThread = new Thread(ExecGetMarketInfo);
            }
            else
            {
                if (getMarketInfoThread.IsAlive)
                {
                    try
                    {
                        getMarketInfoThread.Abort();
                    }
                    catch (Exception)
                    {
                        //Abort exception, Ignore
                    }
                }
                getMarketInfoThread = new Thread(ExecGetMarketInfo);
            }
            getMarketInfoThread.Start();
            
            if (getUserInfoThread == null || !getUserInfoThread.IsAlive)
            {
                getUserInfoThread = new Thread(ExecGetUserInfos);
            }
            else
            {
                if (getUserInfoThread.IsAlive)
                {
                    try
                    {
                        getUserInfoThread.Abort();
                    }
                    catch (Exception)
                    {
                        //Abort exception, Ignore
                    }
                }
                getUserInfoThread = new Thread(ExecGetUserInfos);
            }
            getUserInfoThread = new Thread(ExecGetUserInfos);
            getUserInfoThread.Start();
        }

        private void DragonExMiningTool_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (getMarketInfoThread != null && getMarketInfoThread.IsAlive)
            {
                try
                {
                    getMarketInfoThread.Abort();
                }
                catch (Exception)
                {
                    //Abort exception, Ignore
                }
            }
            if (getUserInfoThread != null && getUserInfoThread.IsAlive)
            {
                try
                {
                    getUserInfoThread.Abort();
                }
                catch (Exception)
                {
                    //Abort exception, Ignore
                }
            }
            if (tradingThread != null && tradingThread.IsAlive)
            {
                try
                {
                    // If is trading, abort
                    tradingThread.Abort();
                }
                catch (Exception)
                {
                    //Abort exception, Ignore
                }
            }
        }

        private void pairCmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            string pair = pairCmb.SelectedValue.ToString();
            if (!string.IsNullOrEmpty(pair) 
                && ConfigTool.TradePairDict.ContainsKey(pair)
                && !string.Equals(ConfigTool.CurrentPair.Key, pair))
            {
                ConfigTool.CurrentPair = new KeyValuePair<string, int>(pair, ConfigTool.TradePairDict[pair]);

                var baseCoinArray = ConfigTool.CurrentPair.Key.Split('_');
                ConfigTool.CurrentBase = baseCoinArray[1];
                ConfigTool.CurrentCoin = baseCoinArray[0];

                aAccBaseLbl.Text = ConfigTool.CurrentBase.ToUpper() + ":";
                aAccCoinLbl.Text = ConfigTool.CurrentCoin.ToUpper() + ":";
                bAccBaseLbl.Text = ConfigTool.CurrentBase.ToUpper() + ":";
                bAccCoinLbl.Text = ConfigTool.CurrentCoin.ToUpper() + ":";
                profitsAllBaseLbl.Text = "All " + ConfigTool.CurrentBase.ToUpper() + ":";
                profitsAllCoinLbl.Text = "All " + ConfigTool.CurrentCoin.ToUpper() + ":";

                pairChanged = true;
                if (getUserInfoThread != null && getUserInfoThread.IsAlive)
                {
                    try
                    {
                        getUserInfoThread.Abort();
                    }
                    catch (Exception)
                    {
                        //Abort exception, Ignore
                    }
                }
                getUserInfoThread = new Thread(ExecGetUserInfos);
                getUserInfoThread.Start();
            }
        }

        private void setBaseBtn_Click(object sender, EventArgs e)
        {
            profits.UpdateBase(dragonExApiForA.UserAmounts, dragonExApiForB.UserAmounts);
            ResetCurrentUserInfos();
        }

        private void updateUserInfoBtn_Click(object sender, EventArgs e)
        {
            GetUserInfo();
        }

        private void autoTradeChk_CheckedChanged(object sender, EventArgs e)
        {
            bool isSet = true;
            if (autoTradeChk.Checked)
            {
                //Check if params is correct
                if (!autoGenerateMinePrice)
                {
                    //When not auto generate, need to check the ptice is between current buy and sell price
                    if(dragonExApiForA.Tde.MaxBid.Price >= minePrice 
                        || dragonExApiForA.Tde.MinAsk.Price <= minePrice)
                    {
                        isSet = false;
                        autoTradeChk.Checked = false;
                        MessageBox.Show("Mine price should between min ask and max bid.Please check and try again.",
                            "Incorrect parameters", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                }
                if (!mineAmountUnlimited)
                {
                    //When mine amount is not unlimited, need to check mine amount is over minimum amount
                    //To ensure trading successfully, check all sell and buy price and amount is over
                    if(dragonExApiForA.Tde.MaxBid.Price * mineAmount <= ConfigTool.MinimumTradeUsdtAmount
                        || dragonExApiForA.Tde.MinAsk.Price * mineAmount <= ConfigTool.MinimumTradeUsdtAmount)
                    {
                        isSet = false;
                        autoTradeChk.Checked = false;
                        MessageBox.Show("Mine Amount and Price should over 1 usdt.Please check and try again."
                            + Environment.NewLine + "To ensure trading successfully, the price is base on minimum ask price.",
                            "Incorrect parameters", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                }
                if (!tradeIntervalUnlimited)
                {
                    //When trade interval is not unlimited, need to check whether trade interval is correct
                    if(tradeInterval <= 0)
                    {
                        isSet = false;
                        autoTradeChk.Checked = false;
                        MessageBox.Show("Trade Interval is incorrect.Please check and try again.",
                            "Incorrect parameters", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                }
            }
            if (isSet)
            {
                autoTrading = autoTradeChk.Checked;
                tradeBtn.Enabled = !autoTrading;
                pairCmb.Enabled = !autoTrading;
                autoGenerateChk.Enabled = !autoTrading;
                mineAmountUnlimitedChk.Enabled = !autoTrading;
                tradeIntervalUnlimitedChk.Enabled = !autoTrading;
                minePriceNud.Enabled = !autoTrading && !autoGenerateChk.Checked;
                mineAmountNud.Enabled = !autoTrading && !mineAmountUnlimitedChk.Checked;
                tradeIntervalNud.Enabled = !autoTrading && !tradeIntervalUnlimitedChk.Checked;
            }
        }

        private void autoGenerateChk_CheckedChanged(object sender, EventArgs e)
        {
            autoGenerateMinePrice = autoGenerateChk.Checked;
            if (!autoGenerateMinePrice)
            {
                minePriceNud.Value = previousMinePrice;
            }
            minePriceNud.Enabled = !autoGenerateMinePrice;
        }

        private void mineAmountUnlimitedChk_CheckedChanged(object sender, EventArgs e)
        {
            mineAmountUnlimited = mineAmountUnlimitedChk.Checked;
            if (!mineAmountUnlimited)
            {
                mineAmountNud.Value = previousMineAmount;
            }
            mineAmountNud.Enabled = !mineAmountUnlimited;
        }

        private void tradeIntervalUnlimitedChk_CheckedChanged(object sender, EventArgs e)
        {
            tradeIntervalUnlimited = tradeIntervalUnlimitedChk.Checked;
            tradeIntervalNud.Enabled = !tradeIntervalUnlimited;
        }

        private void minePriceNud_ValueChanged(object sender, EventArgs e)
        {
            if (!autoGenerateMinePrice)
            {
                minePrice = minePriceNud.Value;
                previousMinePrice = minePrice;
            }
        }

        private void mineAmountNud_ValueChanged(object sender, EventArgs e)
        {
            if (!mineAmountUnlimited)
            {
                mineAmount = mineAmountNud.Value;
                previousMineAmount = mineAmount;
            }
        }

        private void tradeIntervalNud_ValueChanged(object sender, EventArgs e)
        {
            if (!tradeIntervalUnlimited)
            {
                tradeInterval = (int)tradeIntervalNud.Value;
            }
        }

        private void tradeBtn_Click(object sender, EventArgs e)
        {
            if(tradeSide != TradeSide.A_TO_B && tradeSide != TradeSide.B_TO_A)
            {
                // If Not A=>B and B=>A
                MessageBox.Show("Use without auto trade, the side should be A_To_B or B_To_A.", 
                    "Check Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            tradeBtn.Enabled = false;
            logTxt.Text = "";
            lastTradeTime = 0;
            ExecTrading();
            if (tradingThread == null || !tradingThread.IsAlive)
            {
                tradeBtn.Enabled = true;
            }
        }

        private void aToBSideRb_CheckedChanged(object sender, EventArgs e)
        {
            if (aToBSideRb.Checked)
            {
                tradeSide = TradeSide.A_TO_B;
            }
        }

        private void aBAFSideRb_CheckedChanged(object sender, EventArgs e)
        {
            if (aBAFSideRb.Checked)
            {
                tradeSide = TradeSide.BOTH_A_B_A_FIRST;
                tradeMethod = TradeMethod.A_TO_B;
            }
        }

        private void bToASideRb_CheckedChanged(object sender, EventArgs e)
        {
            if (bToASideRb.Checked)
            {
                tradeSide = TradeSide.B_TO_A;
            }
        }

        private void aBBFSideRb_CheckedChanged(object sender, EventArgs e)
        {
            if (aBBFSideRb.Checked)
            {
                tradeSide = TradeSide.BOTH_A_B_B_FIRST;
                tradeMethod = TradeMethod.B_TO_A;
            }
        }

        private delegate void ResetTradingInfos(string logMsg, bool isEnd = false);

        /// <summary>
        /// Reset trading log
        /// </summary>
        /// <param name="logMsg">Log message</param>
        /// <param name="isEnd">Whether is end</param>
        private void ResetTradingLog(string logMsg, bool isEnd = false)
        {
            if (string.IsNullOrEmpty(logTxt.Text))
            {
                logTxt.Text = logMsg;
            }
            else
            {
                logTxt.Text = logTxt.Text + Environment.NewLine + logMsg;
            }
            if (isEnd)
            {
                if (!autoTrading)
                {
                    tradeBtn.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Execute trading A=>B
        /// </summary>
        public void ExecTradingAB()
        {
            try
            {
                var logMsg = "Start A=>B Trading with " + DateTime.Now.ToString("yyyyMMdd HHmmss") + Environment.NewLine
                    + "A Sell:" + tradingEntity.Sell.ToString() + "  Amount:" + tradingEntity.Amount.ToString();
                LogTool.LogTradeInfo(logMsg, LogLevels.TRACE);
                this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, false });
                //Start A Sell
                var qte = dragonExApiForA.TradeCoinToBase(ConfigTool.CurrentPair.Value,
                    tradingEntity.Sell, tradingEntity.Amount);
                if (qte != null)
                {
                    dragonExApiForA.UserAmounts.CoinAmount -= CommonUtils.GetTruncateDecimal(tradingEntity.Amount, ConfigTool.Digits);
                    dragonExApiForA.UserAmounts.BaseAmount += CommonUtils.GetTruncateDecimal(
                        tradingEntity.Amount * tradingEntity.Sell * (1 - ConfigTool.TradeFee), ConfigTool.Digits);
                    this.Invoke(new ResetUI(ResetCurrentUserInfos), new object[] { });

                    logMsg = "A Sell Succeed" + Environment.NewLine
                        + "B Buy:" + tradingEntity.Buy.ToString() + "  Amount:" + tradingEntity.Amount.ToString();
                    LogTool.LogTradeInfo(logMsg, LogLevels.TRACE);
                    this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, false });
                    //Start B Buy
                    var te = dragonExApiForA.TradeBaseToCoin(ConfigTool.CurrentPair.Value, 
                        tradingEntity.Buy, tradingEntity.Amount);

                    if (te == null)
                    {
                        for (int tryCount = 0; tryCount < ConfigTool.TradeTryCountWhenSellSucceed; tryCount++)
                        {
                            te = dragonExApiForA.TradeBaseToCoin(ConfigTool.CurrentPair.Value, 
                                tradingEntity.Buy, tradingEntity.Amount);
                            if (te != null)
                            {
                                logMsg = "B Buy failed.Retry " + tryCount.ToString() + " succeed";
                                LogTool.LogTradeInfo(logMsg, LogLevels.TRACE);
                                this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, false });
                                break;
                            }
                            else
                            {
                                logMsg = "B Buy failed.Retry " + tryCount.ToString() + " failed.";
                                LogTool.LogTradeInfo(logMsg, LogLevels.ERROR);
                                this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, false });
                            }
                            Thread.Sleep(200);
                        }
                    }

                    if (te != null)
                    {
                        logMsg = "B Buy Succeed";
                        LogTool.LogTradeInfo(logMsg, LogLevels.TRACE);
                        this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, false });

                        dragonExApiForB.UserAmounts.CoinAmount += CommonUtils.GetTruncateDecimal(tradingEntity.Amount, ConfigTool.Digits);
                        dragonExApiForB.UserAmounts.BaseAmount -= CommonUtils.GetTruncateDecimal(
                            tradingEntity.Amount * tradingEntity.Buy * (1 + ConfigTool.TradeFee), ConfigTool.Digits);
                        this.Invoke(new ResetUI(ResetCurrentUserInfos), new object[] { });

                        //Wait 5s for trading succeed
                        Thread.Sleep(5000);
                        //try 5 count check
                        bool aResult, bResult;
                        aResult = false;
                        bResult = false;
                        for (int tryCount = 0; tryCount < ConfigTool.ConfirmTryCountForTradeSucceed; tryCount++)
                        {
                            if (!aResult)
                            {
                                aResult = dragonExApiForA.CheckOrderSucceed(qte.OrderId);
                                if (aResult)
                                {
                                    logMsg = "Check A Sell Succeed";
                                    this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, false });
                                }
                            }
                            if (!bResult)
                            {
                                bResult = dragonExApiForB.CheckOrderSucceed(te.OrderId);
                                if (bResult)
                                {
                                    logMsg = "Check B Buy Succeed";
                                    this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, false });
                                }
                            }
                            if (aResult && bResult)
                            {
                                //If all succeeded, exit try
                                break;
                            }
                            Thread.Sleep(2000);
                        }

                        logMsg = "";
                        this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, true });
                        
                        GetUserInfo();
                    }
                    else
                    {
                        logMsg = "B Buy Failed." + Environment.NewLine + "Abort Trading";
                        LogTool.LogTradeInfo(logMsg, LogLevels.TRACE);
                        this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, true });
                    }
                }
                else
                {
                    logMsg = "A Sell Failed." + Environment.NewLine + "Abort Trading";
                    LogTool.LogTradeInfo(logMsg, LogLevels.TRACE);
                    this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, true });
                }
            }
            catch (ThreadAbortException)
            {
                //Do nothing when aborting thread
                var logMsg = "Trading thread is immediately aborted.Trading failed.";
                LogTool.LogTradeInfo(logMsg, LogLevels.ERROR);
                this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { "Failed", true });
            }
            catch (Exception ex)
            {
                this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { "Failed:" + ex.Message, true });
                //Error happened when getting
                var logMsg = "Exception happened when Trading B to A.Details:" + Environment.NewLine
                    + ex.Message + Environment.NewLine + ex.StackTrace;
                Console.WriteLine(logMsg);
                LogTool.LogTradeInfo(logMsg, LogLevels.ERROR);
            }
            finally
            {
                lastTradeTime = DateTime.Now.Ticks;
            }
        }

        /// <summary>
        /// Execute trading B=>A
        /// </summary>
        public void ExecTradingBA()
        {
            try
            {
                var logMsg = "Start B=>A Trading with " + DateTime.Now.ToString("yyyyMMdd HHmmss") + Environment.NewLine
                    + "B Sell:" + tradingEntity.Sell.ToString() + "  Amount:" + tradingEntity.Amount.ToString();
                LogTool.LogTradeInfo(logMsg, LogLevels.TRACE);
                this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, false });
                //Start B Sell
                var te = dragonExApiForB.TradeCoinToBase(ConfigTool.CurrentPair.Value, 
                    tradingEntity.Sell, tradingEntity.Amount);
                if (te != null)
                {
                    dragonExApiForB.UserAmounts.CoinAmount -= CommonUtils.GetTruncateDecimal(tradingEntity.Amount, ConfigTool.Digits);
                    dragonExApiForB.UserAmounts.BaseAmount += CommonUtils.GetTruncateDecimal(
                        tradingEntity.Amount * tradingEntity.Sell * (1 - ConfigTool.TradeFee), ConfigTool.Digits);
                    this.Invoke(new ResetUI(ResetCurrentUserInfos), new object[] { });

                    logMsg = "B Sell Succeed" + Environment.NewLine
                        + "A Buy:" + tradingEntity.Buy.ToString() + "  Amount:" + tradingEntity.Amount.ToString();
                    LogTool.LogTradeInfo(logMsg, LogLevels.TRACE);
                    this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, false });
                    //Start A Buy
                    var qte = dragonExApiForA.TradeBaseToCoin(ConfigTool.CurrentPair.Value,
                        tradingEntity.Buy, tradingEntity.Amount);

                    if (qte == null)
                    {
                        for (int tryCount = 0; tryCount < ConfigTool.TradeTryCountWhenSellSucceed; tryCount++)
                        {
                            qte = dragonExApiForA.TradeBaseToCoin(ConfigTool.CurrentPair.Value, 
                                tradingEntity.Buy, tradingEntity.Amount);
                            if (qte != null)
                            {
                                logMsg = "A Buy failed.Retry " + tryCount.ToString() + " succeed";
                                LogTool.LogTradeInfo(logMsg, LogLevels.TRACE);
                                this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, false });
                                break;
                            }
                            else
                            {
                                logMsg = "A Buy failed.Retry " + tryCount.ToString() + " failed.";
                                LogTool.LogTradeInfo(logMsg, LogLevels.ERROR);
                                this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, false });
                            }
                            Thread.Sleep(200);
                        }
                    }

                    if (qte != null)
                    {
                        logMsg = "A Buy Succeed";
                        LogTool.LogTradeInfo(logMsg, LogLevels.TRACE);
                        this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, false });

                        dragonExApiForA.UserAmounts.CoinAmount += CommonUtils.GetTruncateDecimal(tradingEntity.Amount, ConfigTool.Digits);
                        dragonExApiForA.UserAmounts.BaseAmount -= CommonUtils.GetTruncateDecimal(
                            tradingEntity.Amount * tradingEntity.Buy * (1 + ConfigTool.TradeFee), ConfigTool.Digits);
                        this.Invoke(new ResetUI(ResetCurrentUserInfos), new object[] { });

                        //try * count check
                        bool aResult, bResult;
                        aResult = false;
                        bResult = false;
                        for (int tryCount = 0; tryCount < ConfigTool.ConfirmTryCountForTradeSucceed; tryCount++)
                        {
                            Thread.Sleep(2000);
                            if (!bResult)
                            {
                                bResult = dragonExApiForB.CheckOrderSucceed(te.OrderId);
                                if (bResult)
                                {
                                    logMsg = "Check B Sell Succeed";
                                    this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, false });
                                }
                            }
                            if (!aResult)
                            {
                                aResult = dragonExApiForA.CheckOrderSucceed(qte.OrderId);
                                if (aResult)
                                {
                                    logMsg = "Check A Buy Succeed";
                                    this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, false });
                                }
                            }
                            if (aResult && bResult)
                            {
                                //If all succeeded, exit try
                                break;
                            }
                        }
                        logMsg = "";
                        this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, true });

                        GetUserInfo();
                    }
                    else
                    {
                        logMsg = "A Buy Failed." + Environment.NewLine + "Abort Trading";
                        LogTool.LogTradeInfo(logMsg, LogLevels.TRACE);
                        this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, true });
                    }
                }
                else
                {
                    logMsg = "B Sell Failed." + Environment.NewLine + "Abort Trading";
                    LogTool.LogTradeInfo(logMsg, LogLevels.TRACE);
                    this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { logMsg, true });
                }
            }
            catch (ThreadAbortException)
            {
                //Do nothing when aborting thread
                var logMsg = "Trading thread is immediately aborted.Trading failed.";
                LogTool.LogTradeInfo(logMsg, LogLevels.ERROR);
                this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { "Failed", true });
            }
            catch (Exception ex)
            {
                this.Invoke(new ResetTradingInfos(ResetTradingLog), new object[] { "Failed:" + ex.Message, true });
                //Error happened when getting
                var logMsg = "Exception happened when Trading A to B.Details:" + Environment.NewLine
                    + ex.Message + Environment.NewLine + ex.StackTrace;
                Console.WriteLine(logMsg);
                LogTool.LogTradeInfo(logMsg, LogLevels.ERROR);
            }
            finally
            {
                lastTradeTime = DateTime.Now.Ticks;
            }
        }
    }
}
