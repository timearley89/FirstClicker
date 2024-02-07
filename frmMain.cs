﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Media;
using System.Numerics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Serialization;
using MoneyMiner.Controls;
using Microsoft.Win32;
using MoneyMiner;
using MoneyMiner.Properties;
using MoneyMiner.Windows;
using static MoneyMiner.Upgrade;
using System.Security.Policy;
using System.Net.Security;
using Earleytech;
using static Earleytech.Strings;
using System.Windows.Forms.DataVisualization.Charting;
using System.Data.SqlClient;

namespace MoneyMiner
{

    public partial class frmMain : Form
    {
        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        static extern Int32 mciSendString(string command, StringBuilder? buffer, int bufferSize, IntPtr hwndCallback);
        /*NOTES & TODO

        //Money needs to 'do' something, or be used for something. Would add some depth, but what? In 'other games', money is used to buy 'Mega-bucks', which can be
            used to purchase global multipliers, time warps, etc.

        //Should have bonuses like time warps, profit multipliers, autoclickers, etc. We have the framework now to implement them pretty easily.

        //Achievements? Or not necessary?

        //Need more upgrades! & Rebalance upgrades. Add more clickAmount upgrades and change multipliers to keep items relevant.

        //Need Prestige Upgrades as well - as in upgrades you buy with prestige points. -Maybe, Maybe not...

        //Move default items to external (xml?) file with permissions (create from internal default if it doesn't exist or is outdated - compare application.settings.builddate with file date?)

        //Better separate logic from visuals, so much so that visual update speed can be set arbitrarily and it won't affect how the game runs. May be a large task. Use async/await preferably.
        
        //In btnPrestige flashing method, when user views prestige and clicks 'no', set the bonus target int to the current projected bonus(Math.Floor((projectedBonus / 100.0d) + 1) to prevent having
        //to 'catch up' to projections.

        //For the purpose of balancing, should I implement an 'autobuyer' that runs based on the value of a private readonly const autoBuy, and
        //purchases items and upgrades as it can afford them? I could then speed up the game using cheatengine and watch the entirety of gameplay, 
        //potentially logging salary/sec data from each itemview and graphing it after a certain threshold is reached. This would of course be for
        //internal testing and debug ONLY. It could run from visualupdate_tick.

         */

        //----Properties/Fields----//
        public Game myGame;
        public string BuildVersion = "1.4.1.0-alpha";
        public string logfile;
        public bool PrestigeUpdateHasBeenView = true;
        public int PrestigeBonusGoal = 2;
        public const int MinSalaryTimeMS = 200;
        public StringifyOptions NumberViewSetting = StringifyOptions.LongText;

        private const bool UseAutobuy = false;
        GraphViewer mygraph;
        Series upgradedata;

        //----Initialization----//
        public frmMain()
        {
            InitializeComponent();
            mygraph = new();
            upgradedata = new("Upgrades");
            upgradedata.Color = Color.Aqua;
            upgradedata.ChartType = SeriesChartType.Spline;
            upgradedata.ChartArea = "Miner $/Sec";
            
            logfile = CreateLog();
            //attempt autoload of $"{Environment.CurrentDirectory}\GameState.mmf"
            GameState? tempSave = this.LoadGame();
            bool wasSaveNull = false;
            if (tempSave != null)
            {
                //load data from tempsave and use it to initialize.
                myGame = new Game(tempSave);
                if (myGame.PrestigeNextRestart && myGame.prestigeGainedNextRestart > 0.0d && tempSave.saveType == SaveType.Prestigesave)
                {
                    this.myGame = ApplyNewPrestige(myGame);
                }
            }
            else
            {
                //load default data and initialize.
                wasSaveNull = true;
                myGame = new Game();
            }
            myGame.CurrentLogFile = logfile;
            LogMessage(wasSaveNull ? "New Game Initialized" : "Game Save Loaded");
            CheckForXml();
            InitAudio();
            InitControls();
            LoadItemControlsToForm();
            LoadUpgradeControlsToForm();

        }
        public void CheckForXml()
        {
            LogMessage("Checking for Upgrades.xml...");
            if (File.Exists(Environment.CurrentDirectory + @"\Resources\Xml\Upgrades.xml"))
            {
                LogMessage("Upgrades.xml found. Loading...");
                myGame.MainUpgradeList = MergeUpgradesFromXml(myGame.MainUpgradeList, LoadUpgradesFromXml(Environment.CurrentDirectory + @"\Resources\Xml\Upgrades.xml"));
            }
            else
            {
                LogMessage("Upgrades.xml not found. Creating...");
                if (!Directory.Exists(Environment.CurrentDirectory + @"\Resources\Xml\")) { Directory.CreateDirectory(Environment.CurrentDirectory + @"\Resources\Xml\"); }
                SaveUpgradesToXml(myGame.MainUpgradeList, Environment.CurrentDirectory + @"\Resources\Xml\Upgrades.xml");
            }
            LogMessage("Complete. Continuing...");
        }
        public void InitAudio()
        {
            LogMessage("Initializing Audio...");
            mciSendString($@"open {Environment.CurrentDirectory}\Resources\cashregisterpurchase.wav type mpegvideo alias registersound", null, 0, IntPtr.Zero);
            mciSendString($@"open {Environment.CurrentDirectory}\Resources\clickbutton.wav type mpegvideo alias clicksound", null, 0, IntPtr.Zero);
            mciSendString($@"open {Environment.CurrentDirectory}\Resources\pickaxe-clank-01.wav type mpegvideo alias pickaxe1sound", null, 0, IntPtr.Zero);
            mciSendString($@"open {Environment.CurrentDirectory}\Resources\pickaxe-clank-02.wav type mpegvideo alias pickaxe2sound", null, 0, IntPtr.Zero);
            mciSendString($@"open {Environment.CurrentDirectory}\Resources\pickaxe-clank-03.wav type mpegvideo alias pickaxe3sound", null, 0, IntPtr.Zero);
            mciSendString($@"open {Environment.CurrentDirectory}\Resources\pickaxe-clank-04.wav type mpegvideo alias pickaxe4sound", null, 0, IntPtr.Zero);
            mciSendString($@"open {Environment.CurrentDirectory}\Resources\pickaxe-clank-05.wav type mpegvideo alias pickaxe5sound", null, 0, IntPtr.Zero);
            mciSendString($@"open {Environment.CurrentDirectory}\Resources\pickaxe-clank-06.wav type mpegvideo alias pickaxe6sound", null, 0, IntPtr.Zero);
            mciSendString($@"open {Environment.CurrentDirectory}\Resources\pickaxe-clank-07.wav type mpegvideo alias pickaxe7sound", null, 0, IntPtr.Zero);
            mciSendString($@"open {Environment.CurrentDirectory}\Resources\pickaxe-clank-08.wav type mpegvideo alias pickaxe8sound", null, 0, IntPtr.Zero);
            mciSendString($@"open {Environment.CurrentDirectory}\Resources\BackgroundMusic01.mp3 type mpegvideo alias backgroundmusic01", null, 0, IntPtr.Zero);
            mciSendString($@"open {Environment.CurrentDirectory}\Resources\cashregisterpurchase2.wav type mpegvideo alias registersound2", null, 0, IntPtr.Zero);
            mciSendString($@"open {Environment.CurrentDirectory}\Resources\ping.wav type mpegvideo alias pingsound", null, 0, IntPtr.Zero);
            LogMessage("Audio Initialized");
        }   //before frmMain_load
        public void InitControls()
        {
            LogMessage("Initializing Controls...");
            //Set default form colors
            this.BackColor = Colors.colBackground;
            //this.btnMine.BackColor = Colors.colButtonEnabled;
            btnMine.Parent = pctCenterBackground;
            btnMine.Left = (pctCenterBackground.Width / 2) - (((int)btnMine.Width) / 2);
            btnMine.Top = pctCenterBackground.Height - btnMine.Height - lblMatsMined.Height;

            btnMine.FlatStyle = FlatStyle.Flat;
            btnMine.FlatAppearance.BorderSize = 0;
            btnMine.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnMine.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnMine.BackColor = Color.Transparent;
            btnMine.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            btnMine.ForeColor = Colors.colButtonEnabled;
            this.btnPrestige.BackColor = Colors.colButtonEnabled;
            this.btnPurchAmount.BackColor = Colors.colButtonEnabled;
            this.btnQuickBuy.BackColor = Colors.colButtonDisabled;
            this.btnQuickBuy.ForeColor = Colors.colUpgradeTextDisabled;
            this.btnQuickBuy.Enabled = false;   //only enable if we have upgrade(s) available to buy, and we can afford them.
            this.itemPanel.BackColor = Colors.colBorders;
            this.UpgradePanel.BackColor = Colors.colBorders;
            this.grpMoney.BackColor = Colors.colBorders;
            this.btnPause.BackColor = Colors.colButtonEnabled;
            this.btnMine.BackgroundImageLayout = ImageLayout.Stretch;
            this.btnUnlocks.BackColor = Colors.colButtonEnabled;
            this.btnUnlocks.ForeColor = Colors.colUpgradeTextEnabled;
            LogMessage("Controls Initialized");
        }   //after ctor, before frmMain_load
        public void LoadItemControlsToForm()
        {

            LogMessage("Loading Items To Form...");
            this.NumberViewSetting = myGame.numberviewsetting;
            //Populate form with items from array, then center the items.
            for (int i = 1; i <= myGame.myItems.Length; i++)
            {
                myGame.myItems[i - 1].NumViewSetting = this.NumberViewSetting;
                itemPanel.Controls.Add(myGame.myItems[i - 1]);  //add all items to itempanel
            }
            itemPanel_Resize(this, EventArgs.Empty);  //make sure items are centered
            LogMessage("Items Loaded");
        }   //after ctor, before frmMain_load
        public void LoadUpgradeControlsToForm()
        {
            LogMessage("Loading Upgrades to Form...");
            //add upgrade button for each upgrade, configure it, and add to form's upgradePanel, then center it.
            for (int i = 0; i < myGame.MainUpgradeList.Count; i++)
            {
                Upgrade upgrade = myGame.MainUpgradeList[i];
                UpgradeButton btn = new UpgradeButton();
                btn.Text = upgrade.Description + $"\n${Stringify(upgrade.Cost.ToString("R"), this.NumberViewSetting):N}";
                btn.Font = new Font("Ebrima", 9, FontStyle.Bold);
                btn.MouseHover += Btn_Hover;
                btn.myUpgrade = upgrade;
                btn.CausesValidation = false;
                UpgradeButtonEnable(btn, false);
                myGame.upgradeButtons.Add(btn);
            }
            //Configure each button and add to form's UpgradePanel
            for (int i = 0; i < myGame.upgradeButtons.Count; i++)      //SET UP UPGRADE BUTTONS
            {
                //itemID==0 is clickAmount upgrade button, itemID==1-8 is item upgrade button.
                //itemID==15 is allitem upgrade, itemID==20 is prestigemultiplier upgrade.

                myGame.upgradeButtons[i].Click += new EventHandler(upgradeClicked);
                myGame.upgradeButtons[i].Width = (int)(UpgradePanel.Width * 0.85);  //button width is 85% of panel width
                myGame.upgradeButtons[i].Height = (int)(myGame.upgradeButtons[i].Width * 0.40); //button height is 40% of button width
                UpgradePanel.Controls.Add(myGame.upgradeButtons[i]);   //add all upgrade buttons to upgradepanel
            }


            UpgradePanel_Resize(this, EventArgs.Empty);   //make sure upgrade buttons are centered
            LogMessage("Upgrades Loaded");
        }   //after ctor, before frmMain_load
        private void frmMain_Load(object sender, EventArgs e)
        {

            LogMessage("Form loaded");
            //set window state according to game object, and thus by the last save, if it exists
            this.WindowState = myGame.myWindowState;
            //initaudio
            SetAudioVolume(myGame.MusicVolume, myGame.FXVolume);
            this.Refresh();
            LogMessage("Calculating idle earnings and showing message...");
            this.myGame = SinceYouveBeenGone(myGame);
            this.Activate();
            LogMessage("Complete. Starting game...");
            GameStart();
        }
        public void GameStart()
        {
            //start backgroundmusic if enabled or default
            if (myGame.MusicEnabled) { mciSendString("play backgroundmusic01 repeat", null, 0, IntPtr.Zero); }

            //put in gamestart()
            foreach (ItemView item in myGame.myItems)
            {
                item.UpdateLabels();
                if (item.myQty > 0)
                {
                    item.progressMining.Enabled = true;
                    item.progressMining.Value = item.myprogressvalue <= item.progressMining.Maximum ? item.myprogressvalue : item.progressMining.Maximum;

                    item.myTimer.Start();   //If we loaded a game, should we save and calculate interval time vs elapsed real time to get amount of fires to feed to salary?

                }
            }

            this.timerPerSec.Start();
            this.timerVisualUpdate.Start();
            myGame.GameClock.Reset();
            myGame.GameClock.Start();
            if (myGame.myPurchaseAmount == PurchaseAmount.BuyNext)
            {
                myGame.myPurchaseAmount = PurchaseAmount.Buy100;
                btnPurchAmount_Click(this, EventArgs.Empty);
            }
            if (myGame.AutosaveEnabled && myGame.AutosaveInterval > 0) { myGame.AutosaveTimer.Interval = myGame.AutosaveInterval * 60000; myGame.AutosaveTimer.Start(); }
            LogMessage("Game Started");
        }   //after frmMain_load
        public void ClearFormItems()
        {
            LogMessage("Clearing Form Items and Upgrades...");
            this.itemPanel.Controls.Clear();
            this.UpgradePanel.Controls.Clear();
            LogMessage("Form Cleared");
        }

        //----Event Handlers----//
        private void toolTipTick(object? sender, EventArgs e)
        {
            if (myGame.myTip != null) { myGame.myTip.Hide(this); }
            myGame.toolTipTimer.Stop();
            //hide the tooltip and stop the timer. If needed, it will be started again via the hover event.
        }
        private void Btn_Hover(object? sender, EventArgs e)
        {

            myGame.toolTipTimer.Interval = myGame.toolTipVisibleTime;
            myGame.toolTipTimer.Tick += new EventHandler(toolTipTick);

            //get mouse location
            //display some magical floating box that says {Description} multiplies {get-name-from-itemID()}'s salary by {multiplier}
            if (sender == null) { return; }
            if (myGame.myTip != null) { myGame.myTip.Hide(this); }
            myGame.myTip = new ToolTip();

            myGame.myTip.InitialDelay = myGame.toolTipDelay;
            myGame.myTip.IsBalloon = true;
            myGame.myTip.AutoPopDelay = myGame.toolTipVisibleTime;
            myGame.myTip.UseAnimation = true;
            myGame.myTip.UseFading = true; //try it and see?
            UpgradeButton btn = (UpgradeButton)sender;
            //Point mousepos = MousePosition;
            //adjust mousepos for window size and position rather than screen coords
            //a helper method already exists for this...
            Point mousepos = PointToClient(MousePosition);
            mousepos.Offset(-200, -30); //offset the tooltip up 30px and left 100px so the cursor can still click the buttons
            myGame.toolTipTimer.Start();
            if (btn.myUpgrade.itemID >= 1 && btn.myUpgrade.itemID <= myGame.myItems.Length)
            {
                //upgrade refers to an item
                myGame.myTip.Show($"{btn.myUpgrade.Description} multiplies each {myGame.myItems[btn.myUpgrade.itemID - 1].Name}'s salary per second by {btn.myUpgrade.Multiplier}!", this, mousepos);
                //"Double-Tap multiplies Wood's salary per second by 3!"
            }
            else if (btn.myUpgrade.itemID == 0)
            {
                //upgrade is a clickamount upgrade
                myGame.myTip.Show($"{btn.myUpgrade.Description} multiplies 'Click-Mining' earnings by {btn.myUpgrade.Multiplier}!", this, mousepos);
            }
            else if (btn.myUpgrade.itemID == 15)
            {
                //upgrade is for all items
                myGame.myTip.Show($"{btn.myUpgrade.Description} multiplies all miner salaries by {btn.myUpgrade.Multiplier}!", this, mousepos);
            }
            else if (btn.myUpgrade.itemID == 20)
            {
                //upgrade is a prestige point upgrade
                myGame.myTip.Show($"{btn.myUpgrade.Description} adds {((btn.myUpgrade.Multiplier * 100) - 100):N0}% gain per prestige point!", this, mousepos);
            }
            else if (btn.myUpgrade.itemID >= 21 && btn.myUpgrade.itemID <= 28)
            {
                //upgrade is an item speed upgrade
                myGame.myTip.Show($"{btn.myUpgrade.Description} multiplies {myGame.myItems[btn.myUpgrade.itemID - 21].Name} speed by {btn.myUpgrade.Multiplier}!", this, mousepos);
            }
            else
            {
                //we have no idea what this button does. ItemID not found.
                LogMessage($"Invalid UpgradeButton.myUpgrade.itemID encountered in Btn_Hover! Sender: '{btn.Text}', Upgrade: '{btn.myUpgrade.Description}', itemID: {btn.myUpgrade.itemID}");
                return;
            }

        }
        private void timerPerSec_Tick(object sender, EventArgs e)
        {
            //calculate total salary per second
            double tempsal = 0.00d;    //iterate through items owned and calculate total salary per cycle.
            foreach (ItemView view in myGame.myItems)
            {
                tempsal += ((view.mySalary / ((double)view.mySalaryTimeMS / 1000.0d)) * view.myQty);    //if qty is 0, salary increment will be 0.
            }
            myGame.salary = tempsal;
            double prestBonus = myGame.prestigePoints * myGame.prestigeMultiplier;
            double currentcalc = calcPrestige(myGame.lastlifetimeMoney, myGame.thislifetimeMoney);
            if (!PrestigeUpdateHasBeenView && (currentcalc + myGame.prestigePoints) * 2 >= (prestBonus > 0 ? prestBonus : 50.0d) * PrestigeBonusGoal)
            {
                if (btnPrestige.BackColor == Colors.colButtonEnabled)
                {
                    btnPrestige.BackColor = Colors.colButtonPurchased;
                    btnPrestige.ForeColor = Colors.colUpgradeTextPurchased;
                }
                else
                {
                    btnPrestige.BackColor = Colors.colButtonEnabled;
                    btnPrestige.ForeColor = Colors.colUpgradeTextEnabled;
                }
            }
            else if (PrestigeUpdateHasBeenView && (currentcalc + myGame.prestigePoints) * 2 >= (prestBonus > 0 ? prestBonus : 100.0d) * PrestigeBonusGoal)
            {
                PrestigeUpdateHasBeenView = false;
            }
            else if (PrestigeUpdateHasBeenView && btnPrestige.BackColor != Colors.colButtonEnabled)
            {
                btnPrestige.BackColor = Colors.colButtonEnabled;
                btnPrestige.ForeColor = Colors.colUpgradeTextEnabled;
            }
            foreach (ItemView item in myGame.myItems)
            {
                mygraph.myChart.Series.FindByName(item.Name).Points.Add(new DataPoint(myGame.GameClock.Elapsed.TotalSeconds, ItemView.GetTotalSalPerSec(item)));
            }
        }
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            
            LogMessage("Game closing. Shutting down audio...");
            mciSendString("close clicksound", null, 0, IntPtr.Zero);
            mciSendString("close registersound", null, 0, IntPtr.Zero);
            mciSendString("close registersound2", null, 0, IntPtr.Zero);
            mciSendString("close backgroundmusic01", null, 0, IntPtr.Zero);
            for (int i = 1; i < 8; i++)
            {
                mciSendString($"close pickaxe{i}sound", null, 0, IntPtr.Zero);
            }

            if (!myGame.PrestigeNextRestart)
            {
                LogMessage("Audio shut down. Saving...");
                this.SaveGame();
                LogMessage("Save complete. Closing Game...");
                Program.RestartForPrestige = false;
            }
            timerPerSec.Stop();
            timerVisualUpdate.Stop();
            foreach (ItemView item in myGame.myItems)
            {
                item.myTimer.Stop();
                mygraph.myChart.Series.FindByName(item.Name).Points.Add(new DataPoint(myGame.GameClock.Elapsed.TotalSeconds, ItemView.GetTotalSalPerSec(item)));
            }
            if (UseAutobuy)
            {
                
                mygraph.myChart.Series.Add(upgradedata);
                mygraph.ShowDialog();
            }


        }
        private void floatlabelmoved(object? sender, EventArgs e)
        {
            int dimmingamount = 40;
            Label thislbl;
            if (sender != null)
            {
                thislbl = (Label)sender;
            }
            else { return; }
            if (thislbl.ForeColor.A > 0)
            {   //subtract 20 from alpha value

                int alphaval = thislbl.ForeColor.A - dimmingamount >= 0 ? thislbl.ForeColor.A - dimmingamount : 0;
                thislbl.ForeColor = Color.FromArgb(alphaval, Colors.colTextPrimary);
            }
            else
            {
                myGame.floatlabeldeletelist.Add(thislbl);
            }
        }
        public void itemTimer_Tick(object? sender, EventArgs e)
        {
            //sender is the ItemView that contains the timer that 'ticked'. Execution is caught by itemview.salarytimer_tick, and thrown here.
            if (sender == null) { return; }
            ItemView senderview = (ItemView)sender;
            //senderview.myTimer.Stop();
            if (senderview.mySalaryTimeMS <= 200)
            {
                //if the item calling this has the minimum salarytime, just keep the progressbar at maximum. Don't forget to pay the man.
                senderview.progressMining.Value = senderview.progressMining.Maximum;
                myGame.myMoney += senderview.mySalary * senderview.myQty;
                myGame.thislifetimeMoney += senderview.mySalary * senderview.myQty;
                return;
            }
            else { senderview.progressMining.MarqueeAnimationSpeed = senderview.mySalaryTimeMS; }
            if (senderview.progressMining.Value == senderview.progressMining.Maximum)
            {
                //payout, force draw, then iterate
                myGame.myMoney += senderview.mySalary * senderview.myQty;
                myGame.thislifetimeMoney += senderview.mySalary * senderview.myQty;
                senderview.progressMining.Value = 1;
                senderview.progressMining.Value = 0;
                senderview.progressMining.Value += senderview.myTimer.Interval;
                senderview.progressMining.Value--;
            }
            else if (senderview.progressMining.Value < senderview.progressMining.Maximum)
            {
                if (senderview.progressMining.Value + senderview.myTimer.Interval >= senderview.progressMining.Maximum)
                {
                    senderview.progressMining.Value = senderview.progressMining.Maximum;
                }
                else
                {
                    senderview.progressMining.Value += senderview.myTimer.Interval;
                }
                senderview.progressMining.Value--;
                senderview.progressMining.Value++;
            }
            senderview.myprogressvalue = senderview.progressMining.Value;
            /*
             * if max, payout, set to 0 then increment
             * else, increment
             */

            //senderview.myTimer.Start();
        }
        private void frmMain_Resize(object sender, EventArgs e)
        {
            if (this.WindowState != FormWindowState.Minimized) { myGame.myWindowState = this.WindowState; }
        }

        //----Control Interactions----//
        public void upgradeClicked(Object? sender, EventArgs e)
        {

            if (sender == null) { LogMessage("upgradeClicked(sender) was null"); return; } //basic protection
            else
            {
                UpgradeButton btnsender = (UpgradeButton)sender;
                if (!btnsender.IsEnabled) { return; }

                upgradedata.Points.Add(new DataPoint(myGame.GameClock.Elapsed.TotalSeconds, btnsender.myUpgrade.Cost));

                int btnitemID = btnsender.myUpgrade.itemID;
                if (btnitemID >= 1 && btnitemID <= myGame.myItems.Length)
                {
                    //btnvars itemID is between 1 and the last itemID in myItems, so ItemUpgrade
                    if (myGame.myMoney >= btnsender.myUpgrade.Cost)
                    {
                        myGame.myMoney -= btnsender.myUpgrade.Cost;
                        ItemView tempItem = myGame.myItems.Where(x => x.myID == btnitemID).First<ItemView>();
                        tempItem.mySalary *= btnsender.myUpgrade.Multiplier;

                        btnsender.myUpgrade = Upgrade.SetPurchased(btnsender.myUpgrade);
                        //find the upgrade by upgradeid in mainupgradelist that matches the button's upgradeid, and set it's Purchased property, then overwrite it's old entry in mainupgradelist.
                        Upgrade tempUpgrade = myGame.MainUpgradeList.Find(x => x.upgradeID == btnsender.myUpgrade.upgradeID);
                        myGame.MainUpgradeList[myGame.MainUpgradeList.IndexOf(tempUpgrade)] = Upgrade.SetPurchased(tempUpgrade);
                        PlaySound(SoundList.Register);
                        LogMessage($"Upgrade '{btnsender.myUpgrade.Description}' Purchased");
                    }
                }
                else if (btnitemID == 0)
                {
                    //clickAmount upgrade, may be removed entirely, not sure yet
                    if (myGame.myMoney >= btnsender.myUpgrade.Cost)
                    {
                        myGame.myMoney -= btnsender.myUpgrade.Cost;
                        myGame.clickAmount *= btnsender.myUpgrade.Multiplier;
                        //had to make 'SetPurchased' a static method that returned an object reference. For some reason it wasn't updating the object passed to it before...
                        btnsender.myUpgrade = Upgrade.SetPurchased(btnsender.myUpgrade);
                        Upgrade tempUpgrade = myGame.MainUpgradeList.Find(x => x.upgradeID == btnsender.myUpgrade.upgradeID);
                        myGame.MainUpgradeList[myGame.MainUpgradeList.IndexOf(tempUpgrade)] = Upgrade.SetPurchased(tempUpgrade);
                        PlaySound(SoundList.Register);
                        LogMessage($"Upgrade '{btnsender.myUpgrade.Description}' Purchased");
                    }
                }
                else if (btnitemID == 15)
                {
                    //All Items Upgrade
                    if (myGame.myMoney >= btnsender.myUpgrade.Cost)
                    {
                        myGame.myMoney -= btnsender.myUpgrade.Cost;
                        foreach (ItemView item in myGame.myItems)
                        {
                            item.mySalary *= btnsender.myUpgrade.Multiplier;
                        }
                        btnsender.myUpgrade = Upgrade.SetPurchased(btnsender.myUpgrade);
                        Upgrade tempUpgrade = myGame.MainUpgradeList.Find(x => x.upgradeID == btnsender.myUpgrade.upgradeID);
                        myGame.MainUpgradeList[myGame.MainUpgradeList.IndexOf(tempUpgrade)] = Upgrade.SetPurchased(tempUpgrade);
                        PlaySound(SoundList.Register);
                        LogMessage($"Upgrade '{btnsender.myUpgrade.Description}' Purchased");
                    }
                }
                else if (btnitemID == 20)
                {
                    //prestige points upgrade
                    if (myGame.myMoney >= btnsender.myUpgrade.Cost)
                    {
                        double newmult = (btnsender.myUpgrade.Multiplier * 100) - 100;
                        double oldPrestigeMult = myGame.prestigeMultiplier;
                        myGame.prestigeMultiplier += newmult;
                        /*recalculate: 
                         * {
                                each item.salary for new prestigeMultiplier
                                clickAmount for new prestigeMultiplier
                         * }
                         * without invalidating upgrades already purchased, unlocks obtained(not yet implemented), or any other increases/multipliers obtained.
                         * Above, it's calculated by:
                         * this.clickAmount *= ((prestigePoints / (100.0d / prestigeMultiplier)) + 1);
                         * for clickAmount, and by:
                         * item.mySalary *= ((prestigePoints / (100.0d / prestigeMultiplier)) + 1);
                         * for each item salary.
                         * 
                         * 100/3=33.333, 50/33.333 = 1.333, +1 = 2.333, meaning a 133% bonus. Not right, because 50*0.02=100%, so 50*0.03=150%. what about
                         * (((prestmult / 100)*prestpoints)+1)*salary=salary? 3/100=0.03, *prestpoints=1.5, +1=2.5, so 50 points @ 3% boost gives 150% gain (or 250% of salary)?
                         * what if it's 100 points? then boost should be 100*3%=300%, or 400% of salary.
                         * 3/100=0.03, *prestpoints=3, +1=4, which is correct. Lets recalc this.
                         * 
                         * first we do need to determine what 100% of salary would be:
                         * ((oldprestmult / 100)*prestpoints) gives boost percentage.(100%)
                         * 100/(boost+100) => 100/200=0.5, or 100/250=0.4, or 100/724(624% boost!)=0.138125, so on. multiply that by current salary to get initial, then
                         * recalc prestigemult with that value. 1/totalmultiplier (1/2.5) would be the same, or even oldsalary = salary * (1 / ((prestigePoints / (100.0d / oldPrestigeMult)) + 1))
                         * Therefore, 
                         * 
                         * item.mySalary = (item.mySalary * (1 / ((prestigePoints / (100.0d / oldPrestigeMult)) + 1)) * ((prestigePoints / (100.0d / prestigeMultiplier)) + 1));
                         * Can we refactor that equation to reduce it? I might need to dust off some old algebra...
                         * or just use an online factoring tool.
                         * item.mySalary = (item.mySalary * ((prestigeMultiplier * prestigePoints) + 100)) / ((oldPrestigeMult * prestigePoints) + 100);
                         * 
                         * 
                         */
                        foreach (ItemView item in myGame.myItems)
                        {
                            item.mySalary = (item.mySalary * ((myGame.prestigeMultiplier * myGame.prestigePoints) + 100)) / ((oldPrestigeMult * myGame.prestigePoints) + 100);
                        }
                        myGame.clickAmount = (myGame.clickAmount * ((myGame.prestigeMultiplier * myGame.prestigePoints) + 100)) / ((oldPrestigeMult * myGame.prestigePoints) + 100);

                        btnsender.myUpgrade = Upgrade.SetPurchased(btnsender.myUpgrade);
                        Upgrade tempUpgrade = myGame.MainUpgradeList.Find(x => x.upgradeID == btnsender.myUpgrade.upgradeID);
                        myGame.MainUpgradeList[myGame.MainUpgradeList.IndexOf(tempUpgrade)] = Upgrade.SetPurchased(tempUpgrade);
                        PlaySound(SoundList.Register);
                        LogMessage($"Upgrade '{btnsender.myUpgrade.Description}' Purchased");
                    }
                }
                else if (btnitemID >= 21 && btnitemID <= 28)    //Needs to be tested
                {
                    //miner speed x2
                    //this won't break anything with the progressbar/tick system will it? I don't think so...
                    //btnvars itemID is between 21 and 28, so SpeedUpgrade
                    //myItems[i] is derived from btnitemID - 20
                    if (myGame.myMoney >= btnsender.myUpgrade.Cost)
                    {
                        myGame.myMoney -= btnsender.myUpgrade.Cost;
                        ItemView tempItem = myGame.myItems.Where(x => x.myID == btnitemID - 20).First<ItemView>();
                        //If the timeframe will be less than 200mS after upgrade, normalize salary after applying upgrade to 100mS(default).
                        tempItem.mySalaryTimeMS /= (int)btnsender.myUpgrade.Multiplier;
                        if (tempItem.mySalaryTimeMS < MinSalaryTimeMS)
                        {
                            ItemView.NormalizeSalary(tempItem, MinSalaryTimeMS);
                        }
                        else if (tempItem.mySalaryTimeMS % MinSalaryTimeMS != 0)
                        {
                            int newSalTimeMS = ((int)Math.Floor((double)(tempItem.mySalaryTimeMS / MinSalaryTimeMS)) * MinSalaryTimeMS) + MinSalaryTimeMS;
                            ItemView.NormalizeSalary(tempItem, newSalTimeMS);
                        }
                        //set progress to 1 tick, so that the next tick, the item completes a cycle. If black hole has just started and is
                        //then upgraded, yes, it will give a 'free' salary payout. This is intended.
                        tempItem.myprogressvalue = tempItem.mySalaryTimeMS - 100;
                        tempItem.progressMining.Maximum = tempItem.mySalaryTimeMS;
                        tempItem.progressMining.Value = tempItem.myprogressvalue;

                        btnsender.myUpgrade = Upgrade.SetPurchased(btnsender.myUpgrade);
                        //find the upgrade by upgradeid in mainupgradelist that matches the button's upgradeid, and set it's Purchased property, then overwrite it's old entry in mainupgradelist.
                        Upgrade tempUpgrade = myGame.MainUpgradeList.Find(x => x.upgradeID == btnsender.myUpgrade.upgradeID);
                        myGame.MainUpgradeList[myGame.MainUpgradeList.IndexOf(tempUpgrade)] = Upgrade.SetPurchased(tempUpgrade);
                        PlaySound(SoundList.Register);
                        LogMessage($"Upgrade '{btnsender.myUpgrade.Description}' Purchased");
                    }
                }
                UpgradeButtonEnable(btnsender, false);
            }
        }
        public void BuyClicked(ItemView sender)
        {
            
            if (sender.CanAfford(myGame.myMoney, sender.purchaseAmount, sender.myCostMult))
            {

                myGame.myMoney -= sender.calculatedCost;
                PlaySound(SoundList.Register);
                LogMessage($"{sender.purchaseAmount} of Item '{sender.Name}' Purchased");
                sender.myQty += sender.purchaseAmount;
                if (sender.myQty > 0)
                {
                    sender.progressMining.Enabled = true;
                    sender.myTimer.Start();
                }
                sender.myCost = sender.myCost * Math.Pow(sender.myCostMult, sender.purchaseAmount);

                int highestunlockindex = sender.latestUnlock;
                do
                {
                    highestunlockindex++;
                }
                while (sender.myQty >= sender.myUnlockList[0, highestunlockindex]);
                highestunlockindex--;   //since we're checking condition after iteration, we need to back down by 1 to get the last valid result. If we're in this routine, we just bought at least 1. HUI is 0 now.
                if (highestunlockindex > sender.latestUnlock)     //0 > -1 == true
                {
                    //we reached a milestone!
                    do
                    {
                        sender.latestUnlock++;        //-1 + 1 = 0;
                        LogMessage($"'{sender.Name}' Unlock Reached - X{sender.myUnlockList[0, sender.latestUnlock]}");
                        if (sender.myQty > 1) { sender.mySalary *= sender.myUnlockList[1, sender.latestUnlock]; }  //apply the bonus to this item if we bought more than 1, so that we can still have buy1 within buynext, and so we get an unlock for buying 1 of all.
                        bool allOthersHave = true;
                        for (int i = 0; i < myGame.myItems.Length; i++)
                        {
                            if (myGame.myItems[i].myID != sender.myID)   //don't need to check this one again...
                            {
                                if (myGame.myItems[i].myQty < Game.unlockList[sender.latestUnlock])       //if qty < UL[0]==1, then we don't own at least 1 of everything else!
                                {
                                    allOthersHave = false;
                                    break;      //if any one of them fails, exit the loop - it would be computationally wasteful not to.
                                }
                            }
                        }
                        if (allOthersHave)
                        {
                            LogMessage($"Global Unlock Reached - X{Game.unlockList[sender.latestUnlock]}");
                            //double all salaries - global unlock
                            for (int i = 0; i < myGame.myItems.Length; i++)
                            {
                                myGame.myItems[i].mySalary *= Game.unlockMultiplier;
                            }
                            //double clickAmount as well
                            myGame.clickAmount *= 2;
                        }
                    }
                    while (sender.latestUnlock < highestunlockindex); //bonus will be applied, and latestunlock will be set afterwards to equal highestunlockindex.
                                                                      //0 < 0 == false, so execution falls out of loop and continues elsewhere.
                }
                else
                {
                    //they're the same, so we didn't reach a new milestone.
                }
                
            }
            if (myGame.myPurchaseAmount == PurchaseAmount.BuyNext)
            {
                //calculate next
                int temppurchamount;
                if (sender.latestUnlock + 1 < sender.myUnlockList.Length && sender.latestUnlock + 1 >= 0)     //make sure the unlock we're looking for is in the list...
                {
                    temppurchamount = sender.myUnlockList[0, sender.latestUnlock + 1] - sender.myQty;
                }
                else
                {
                    temppurchamount = 1;    //when in doubt, set it to 1. If we've bought all unlocks, we only need to calculate for 1 item purchase.
                }
                sender.calculatedCost = costcalcnew(sender, temppurchamount);
                sender.purchaseAmount = temppurchamount;
            }
            if (sender.mySalaryTimeMS < sender.myTimer.Interval * 2) { ItemView.NormalizeSalary(sender, sender.myTimer.Interval * 2); }
            sender.UpdateLabels();
            sender.ButtonColor(myGame.myMoney, sender.purchaseAmount, sender.myCostMult);
        }
        private void btnPurchAmount_Click(object sender, EventArgs e)
        {
            PlaySound(SoundList.ClickSound);

            //purchAmount should be made into an enum. Perfect use-case for it, and reduces possible errors from invalid values.
            if (myGame.myPurchaseAmount == PurchaseAmount.BuyOne)
            {
                myGame.myPurchaseAmount = PurchaseAmount.BuyTen;
                LogMessage($"Purchase Amount Set to 10");
            }
            else if (myGame.myPurchaseAmount == PurchaseAmount.BuyTen)
            {
                myGame.myPurchaseAmount = PurchaseAmount.Buy100;
                LogMessage($"Purchase Amount Set to 100");
            }
            else if (myGame.myPurchaseAmount == PurchaseAmount.Buy100)
            {
                myGame.myPurchaseAmount = PurchaseAmount.BuyNext;
                LogMessage($"Purchase Amount Set to Next");
            }
            else if (myGame.myPurchaseAmount == PurchaseAmount.BuyNext)
            {
                myGame.myPurchaseAmount = PurchaseAmount.BuyMax;
                LogMessage($"Purchase Amount Set to Max");
            }
            else if (myGame.myPurchaseAmount == PurchaseAmount.BuyMax)
            {
                myGame.myPurchaseAmount = PurchaseAmount.BuyOne;
                LogMessage($"Purchase Amount Set to 1");
            }

            if (myGame.myPurchaseAmount == PurchaseAmount.BuyOne || myGame.myPurchaseAmount == PurchaseAmount.BuyTen || myGame.myPurchaseAmount == PurchaseAmount.Buy100)
            {
                for (int i = 0; i < myGame.myItems.Length; i++)
                {
                    //we just need to update the item with the new amount, and then update the labels and colors.
                    myGame.myItems[i].purchaseAmount = (int)myGame.myPurchaseAmount;
                    myGame.myItems[i].CalcCost(myGame.myItems[i].purchaseAmount, myGame.myItems[i].myCostMult);
                    myGame.myItems[i].ButtonColor(myGame.myMoney, myGame.myItems[i].purchaseAmount, myGame.myItems[i].myCostMult);
                    myGame.myItems[i].UpdateLabels();
                }
            }
            else if (myGame.myPurchaseAmount == PurchaseAmount.BuyMax)
            {
                for (int i = 0; i < myGame.myItems.Length; i++)
                {
                    int temppurchamount = (int)calcMax(myGame.myMoney, myGame.myItems[i]);
                    //myItems[i].CalcCost(temppurchamount, myItems[i].myCostMult);
                    myGame.myItems[i].calculatedCost = costcalcnew(myGame.myItems[i], temppurchamount);
                    myGame.myItems[i].purchaseAmount = temppurchamount;
                    myGame.myItems[i].ButtonColor(myGame.myMoney, myGame.myItems[i].purchaseAmount, myGame.myItems[i].myCostMult);
                    myGame.myItems[i].UpdateLabels();
                }
            }
            else if (myGame.myPurchaseAmount == PurchaseAmount.BuyNext)
            {
                for (int i = 0; i < myGame.myItems.Length; i++)
                {
                    int temppurchamount;
                    if (myGame.myItems[i].latestUnlock + 1 < myGame.myItems[i].myUnlockList.Length && myGame.myItems[i].latestUnlock + 1 >= 0)     //make sure the unlock we're looking for is in the list...
                    {
                        temppurchamount = myGame.myItems[i].myUnlockList[0, myGame.myItems[i].latestUnlock + 1] - myGame.myItems[i].myQty;
                    }
                    else
                    {
                        temppurchamount = 1;    //when in doubt, set it to 1. If we've bought all unlocks, we only need to calculate for 1 item purchase.
                    }
                    myGame.myItems[i].calculatedCost = costcalcnew(myGame.myItems[i], temppurchamount);
                    myGame.myItems[i].purchaseAmount = temppurchamount;
                    myGame.myItems[i].ButtonColor(myGame.myMoney, myGame.myItems[i].purchaseAmount, myGame.myItems[i].myCostMult);
                    myGame.myItems[i].UpdateLabels();
                }
            }
        }
        private void btnMine_Click(object sender, EventArgs e)
        {
            const double matThreshval = 50.0d;  //allows incrperclick to change at different values.
            if (myGame.matsMined != 0 && myGame.matsMined + myGame.incrperclick >= (Math.Round((double)myGame.matsMined / matThreshval, MidpointRounding.ToPositiveInfinity) * matThreshval) && myGame.incrperclick < 50 && myGame.matsMined != (Math.Round((double)myGame.matsMined / matThreshval, MidpointRounding.ToPositiveInfinity) * matThreshval))
            {
                myGame.matsMined += myGame.incrperclick;
                myGame.matsMinedLifetime += myGame.incrperclick;
                myGame.incrperclick++;
                //clickAmount = (clickAmount / (incrperclick - 1)) * incrperclick;

            } //for now, when matsMined reaches another multiple of 100, amountperclick is incremented.
            //incrperclick has a max of 10 for now.
            else { myGame.matsMined += myGame.incrperclick; myGame.matsMinedLifetime += myGame.incrperclick; }

            //We're only calculating clickAmount for one incrperclick. We can add that amount for multiple clicks, but it shouldn't double clickAmount.
            //Later we can use visual and audible cues to signify multiple clicks (like floating text saying 'x2' 'x3' etc, and disappearing after a second, with little 'pop' sounds for each incrperclick in quick succession...)
            myGame.myMoney += myGame.clickAmount * myGame.incrperclick;
            myGame.thislifetimeMoney += myGame.clickAmount * myGame.incrperclick;

            Random randnumber = new Random();
            int i = randnumber.Next(1, 8);
            PlaySound(SoundList.Pickaxe);

            //testing...
            FloatText();
        }
        private void btnPrestige_Click(object sender, EventArgs e)
        {
            PlaySound(SoundList.ClickSound);
            
            //In order for this to work, I need to refactor the main game logic into it's own gameobject that takes parameters for prestige amount, and default params(overrideable) for money, upgrades, purchased items, etc.
            //Or I can take advantage of the load/save system, and just configure the save to reset for a prestige-flagged restart, that way i can customize what parameters get changed.   Edit: Why not both?
            double tempprestige = calcPrestige(myGame.lastlifetimeMoney, myGame.thislifetimeMoney);

            //Once prestige notification has started, if player views it and closes it, the next notification will be current notification bonus threshold + 100%. eg(prestigebonusnotify at 200%, then 300%, then 400%, etc.)
            if (!PrestigeUpdateHasBeenView) 
            { 
                PrestigeUpdateHasBeenView = true;
                if (((tempprestige + myGame.prestigePoints) * 2) / 100 > PrestigeBonusGoal)
                {
                    PrestigeBonusGoal = (int)Math.Floor(((tempprestige + myGame.prestigePoints) * 2) / 100.0d) + 1;
                }
                else
                {
                    PrestigeBonusGoal++;
                }
            }

            foreach (ItemView item in myGame.myItems)
            {
                item.myTimer.Stop();
            }
            this.timerPerSec.Stop();
            this.timerVisualUpdate.Stop();
            myGame.GameClock.Stop();
            myGame.thislifeGameTime += myGame.GameClock.Elapsed;
            myGame.totalGameTime += myGame.GameClock.Elapsed;
            string msg = $"Current Prestige: {Stringify(myGame.prestigePoints.ToString("R"), this.NumberViewSetting)}. \nPrestige to Gain: {Stringify(tempprestige.ToString("R"), this.NumberViewSetting)}. Prestige?";
            MsgBox msgBox = new(msg, "Reset To Earn Prestige Points?");
            DialogResult dres = msgBox.ShowDialog();
            if (dres == DialogResult.Yes)
            {
                this.timerPerSec.Stop();
                this.timerVisualUpdate.Stop();
                myGame.toolTipTimer.Stop();
                myGame.GameClock.Stop();
                foreach (ItemView item in myGame.myItems)
                {
                    item.myTimer.Stop();
                    item.myprogressvalue = item.progressMining.Value;
                }

                Program.RestartForPrestige = true;
                myGame.PrestigeNextRestart = true;

                myGame.prestigeGainedNextRestart = tempprestige;
                LogMessage($"{tempprestige:N0} Prestige Points Added. Saving...");
                SaveGame(myGame.lastSaveLocation);
                LogMessage($"Save Complete. Loading...");
                LoadGame(myGame.lastSaveLocation);
            }
            else if (dres == DialogResult.No)
            {
                foreach (ItemView item in myGame.myItems)
                {
                    if (item.progressMining.Enabled)
                    {
                        item.myTimer.Start();
                    }

                }
                timerPerSec.Start();
                timerVisualUpdate.Start();
                myGame.GameClock.Reset();
                myGame.GameClock.Start();
            }
        }
        private void lblMoney_SizeChanged(object sender, EventArgs e)
        {
            lblMoney.Left = (this.grpMoney.Width - lblMoney.Size.Width) / 2;
        }
        private void lblSalary_SizeChanged(object sender, EventArgs e)
        {
            lblSalary.Left = (this.grpMoney.Width - lblSalary.Size.Width) / 2;
        }
        private void lblClickAmount_SizeChanged(object sender, EventArgs e)
        {
            lblClickAmount.Left = (this.grpMoney.Width - lblClickAmount.Size.Width) / 2;
        }
        public void btnStats_Click(object sender, EventArgs e)
        {
            PlaySound(SoundList.ClickSound);
            //open stats window with current stats, update times and restart gameclock

            myGame.thislifeGameTime += myGame.GameClock.Elapsed;
            myGame.totalGameTime += myGame.GameClock.Elapsed;
            myGame.GameClock.Reset();
            myGame.GameClock.Start();

            string statsmessage =
                $"Salary: ${Stringify(myGame.salary.ToString("R"), this.NumberViewSetting):N} Per Second" +
                $"\nClickAmount: ${Stringify(myGame.clickAmount.ToString("R"), this.NumberViewSetting):N} Per Click" +
                $"\nMoney Earned This Lifetime: ${Stringify(myGame.thislifetimeMoney.ToString("R"), this.NumberViewSetting):N}" +
                $"\nMoney Earned Last Lifetime: ${Stringify(myGame.lastlifetimeMoney.ToString("R"), this.NumberViewSetting):N}" +
                $"\nMoney Earned All Lifetimes: ${Stringify((myGame.lastlifetimeMoney + myGame.thislifetimeMoney).ToString("R"), this.NumberViewSetting):N}" +
                $"\nTime spent this lifetime: {myGame.thislifeGameTime.ToString(@"h\:mm\:ss")}" +
                $"\nTime spent all lifetimes: {myGame.totalGameTime.ToString(@"h\:mm\:ss")}" +
                $"\nPrestige Points: {Stringify(myGame.prestigePoints.ToString("R"), this.NumberViewSetting)}" +
                $"\nPrestige Multiplier: {Stringify(myGame.prestigeMultiplier.ToString("R"), this.NumberViewSetting)}% Per Point" +
                $"\nPrestige Percentage: {Stringify((myGame.prestigePoints * myGame.prestigeMultiplier).ToString("R"), this.NumberViewSetting):N0} %" +
                $"\nMaterials Mined this lifetime: {Stringify(myGame.matsMined.ToString("R"), this.NumberViewSetting):N0}" +
                $"\nMaterials Mined all lifetimes: {Stringify(myGame.matsMinedLifetime.ToString("R"), this.NumberViewSetting):N0}";
            Stats mystats = new Stats(statsmessage);
            mystats.ShowDialog();
        }
        private void btnMine_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender == null) { return; }
            Button thisbutton = (Button)sender;
            if (thisbutton != null && thisbutton.BackgroundImage != null)
            {
                thisbutton.BackgroundImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
            }
            btnMine_Click(sender, EventArgs.Empty);
           
        }
        private void btnMine_MouseUp(object sender, MouseEventArgs e)
        {
            if (sender == null) { return; }
            Button thisbutton = (Button)sender;
            if (thisbutton != null && thisbutton.BackgroundImage != null)
            {
                thisbutton.BackgroundImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
                thisbutton.Refresh();
            }
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                // Handle key at form level.
                btnPause_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        public void FloatText()
        {
            //get center x coord of btnMine

            //create a label just above btnMine

            //add label to list of floatlabels?

            //each visualupdatetick move each label in the list up by some small amount of px

            //create eventhandler for label.locationchanged that changes forecolor to a bit lighter if not transparent

            //if it is transparent, dispose of it
            int mousex = PointToClient(MousePosition).X;
            int centerxofbtn = btnMine.Location.X + (btnMine.Size.Width / 2);
            Label lblNew = new Label();
            lblNew.Location = new Point(mousex, btnMine.Location.Y - 30);
            lblNew.LocationChanged += new EventHandler(floatlabelmoved);
            lblNew.AutoSize = true;
            lblNew.Parent = this;
            lblNew.BringToFront();
            lblNew.Name = $"lblFloat{myGame.floatlabels.Count + 1}";
            lblNew.Text = $"${Stringify((myGame.clickAmount * myGame.incrperclick).ToString(), this.NumberViewSetting):N}";
            lblNew.ForeColor = Color.FromArgb(255, Colors.colTextPrimary);
            lblNew.BackColor = Color.FromArgb(0, Colors.colTextSecondary);
            lblNew.Visible = true;
            lblNew.Enabled = true;
            lblNew.Show();
            myGame.floatlabels.Add(lblNew);
        }
        private void btnPause_Click(object sender, EventArgs e)
        {
            PlaySound(SoundList.ClickSound);
            PauseMenu pauseMenu = new PauseMenu(this as frmMain, myGame);

            pauseMenu.ShowDialog();
        }
        public void ShowAbout()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"MoneyMiner {BuildVersion}");
            //sb.AppendLine($"MoneyMiner {Application.ProductVersion.Split("+")[0]}");
            sb.AppendLine($"\nCreated By Tim Earley");
            sb.AppendLine($"\nLocation: '{Environment.CurrentDirectory}'\n");
            TextReader treader = File.OpenText($@"{Environment.CurrentDirectory}\Resources\ResourceAttributions.txt");
            string filecontents = treader.ReadToEnd();
            string[] textlines = filecontents.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            sb.AppendLine($"\nResource Attributions:\n");
            foreach (string line in textlines)
            {
                if (line != "")
                {
                    if (!line.Contains("http"))
                    {
                        sb.AppendLine($"{line}");
                    }
                }
            }
            sb.AppendLine($"\nThanks for playing MoneyMiner!");
            sb.AppendLine($"\n© 2024 - All Rights Reserved");
            About aboutme = new About(sb.ToString());
            aboutme.ShowDialog();

        }
        public static void btnBuy_Hover(object sender, EventArgs e)
        {
            Button? btnbuy;
            ItemView? btnItem;
            try
            {
                btnbuy = (sender == null) ? null : (Button)sender;
                if (btnbuy == null) { return; }
                btnItem = btnbuy.Parent == null ? null : btnbuy.Parent as ItemView;
                if (btnItem == null) { return; }
            }
            catch (Exception ex)
            {
                if (ex is NullReferenceException) { Debug.WriteLine(ex.Message); }
                else { Debug.WriteLine(ex.Message); }
                return;
            }
            if (btnItem == null) { return; }
            ToolTip salaryGainTip = new ToolTip();
            double currentSalpersec = btnItem.mySalary * btnItem.myQty / (btnItem.mySalaryTimeMS / 1000.0d);
            int mult = 1;
            //calculate total multiplier from item's unlockList if purchased
            if (btnItem.myQty + btnItem.purchaseAmount >= btnItem.myUnlockList[0, btnItem.latestUnlock + 1] && btnItem.latestUnlock + 1 < btnItem.myUnlockList.Length)
            {
                int numberofunlocks = 0;
                //current is 13, pamount=38, totalafter=51
                int unlockcounter = btnItem.latestUnlock + 1;
                do
                {
                    if (btnItem.myQty + btnItem.purchaseAmount >= btnItem.myUnlockList[0, unlockcounter])
                    {
                        numberofunlocks++;
                    }
                    unlockcounter++;
                } while (btnItem.myUnlockList[0, unlockcounter] <= btnItem.myQty + btnItem.purchaseAmount);
                mult = 1;
                int newunlock = btnItem.latestUnlock + 1;
                while (numberofunlocks > 0)
                {
                    mult *= btnItem.myUnlockList[1, newunlock];
                    numberofunlocks--;
                    newunlock++;
                    //we need to do this without invalidating previous multipliers.
                    //I believe multiplier is correct here - we just need to change how we use it afterwards.
                    //((currentsalps + newsalps) * mult) - currentsalps
                }
            }
            double saltogain = btnItem.displaySalPerSec ? 
                (((((btnItem.mySalary * btnItem.purchaseAmount) / (btnItem.mySalaryTimeMS / 1000.0d)) + currentSalpersec) * mult) - currentSalpersec)
                : (((btnItem.mySalary * btnItem.purchaseAmount) + (btnItem.mySalary * btnItem.myQty)) * mult) - (btnItem.mySalary * btnItem.myQty);
            string strGain = btnItem.displaySalPerSec ? $"Gain ${Stringify(saltogain.ToString("R"), btnItem.NumViewSetting):N} per second for {btnItem.Name}!" :
                $"Gain ${Stringify(saltogain.ToString("R"), btnItem.NumViewSetting):N} per cycle for {btnItem.Name}!";
            salaryGainTip.SetToolTip(btnbuy, strGain);
            //salaryGainTip.Show(strGain, Application.OpenForms.OfType<frmMain>().First());
        }
        public void DeleteForever()
        {
            File.Delete(myGame.lastSaveLocation);
            LogMessage($"Save Deleted! Starting New Game...");
            myGame = new Game();
            ClearFormItems();
            InitControls();
            LoadItemControlsToForm();
            LoadUpgradeControlsToForm();
            frmMain_Load(new object(), EventArgs.Empty);
        }
        public void EnableAutosave(bool autosave)
        {
            LogMessage($"Autosave {(autosave ? "Enabled" : "Disabled")}");
            myGame.AutosaveEnabled = autosave;

            if (myGame.AutosaveEnabled && myGame.AutosaveInterval != 0)
            {
                myGame.AutosaveTimer.Tick -= Autosave;
                myGame.AutosaveTimer.Tick += Autosave;
                myGame.AutosaveTimer.Interval = myGame.AutosaveInterval * 60000;
                myGame.AutosaveTimer.Start();
            }
            else
            {
                myGame.AutosaveTimer.Stop();
            }
        }
        public void SetAutosaveInterval(int Interval)
        {
            myGame.AutosaveInterval = Interval;
            if (myGame.AutosaveEnabled && myGame.AutosaveInterval != 0) { myGame.AutosaveTimer.Interval = myGame.AutosaveInterval * 60000; myGame.AutosaveTimer.Start(); }
            else { myGame.AutosaveTimer.Stop(); }
        }
        private void btnQuickBuy_Click(object sender, EventArgs e)
        {
            //when this is enabled and clicked, starting with the lowest cost, buy all upgrades we can afford.
            double currentBalance = myGame.myMoney;
            int upgradeIndex = 0;
            while (currentBalance >= 0.0d && upgradeIndex < myGame.MainUpgradeList.Count - 1)
            {
                //if balance dips below 0 or we've gone through all upgrades, do not enter this loop.
                if (myGame.MainUpgradeList[upgradeIndex].Purchased == false)
                {
                    currentBalance -= myGame.MainUpgradeList[upgradeIndex].Cost;
                }
                upgradeIndex++;
            }
            if (currentBalance < 0) { upgradeIndex--; }
            if (upgradeIndex < 0)
            {
                //we can't buy any upgrades - we shouldn't enter here at all, but you never know...
            }
            for (int i = 0; i <= upgradeIndex; i++)
            {
                //buy upgradelist[i] using existing handler
                upgradeClicked(UpgradePanel.Controls[i], EventArgs.Empty);
            }
            frmMain_UpdateLabels(); //just to ensure the view is updated before user can click something again
        }

        //----Calculations----//
        /// <summary>
        /// Returns calculated amount of prestige points if prestiged right now.
        /// </summary>
        /// <param name="lastlifeMoney">Money made in the last prestige loop</param>
        /// <param name="thislifeMoney">Money made this prestige loop</param>
        /// <param name="prestigeModifier">Modifier for prestige difficulty. Default is 4B, but can be overridden.</param>
        /// <returns>double representing calculated prestige points to gain.</returns>
        public static double calcPrestige(double lastlifeMoney, double thislifeMoney, double prestigeModifier = 400000000000.0d)
        {
            double newlifeMoney = lastlifeMoney + thislifeMoney;
            double term1 = Math.Pow(newlifeMoney / (prestigeModifier / 9), 0.5d);
            double term2 = Math.Pow(lastlifeMoney / (prestigeModifier / 9), 0.5d);
            return term1 - term2 >= 0 ? double.Round(term1 - term2, MidpointRounding.ToZero) : 0;
        }
        public static long calcMax(double balance, ItemView item)
        {
            long maxbuy = (long)Math.Floor((Math.Log(((balance * (item.myCostMult - 1)) / (item.baseCost * Math.Pow(item.myCostMult, item.myQty))) + 1) / Math.Log(item.myCostMult)));
            return maxbuy;
        }
        public static double costcalcnew(ItemView item, int purchqty)
        {
            return ((item.baseCost) * (((Math.Pow(item.myCostMult, item.myQty) * (Math.Pow(item.myCostMult, purchqty) - 1)) / (item.myCostMult - 1))));

        }
        public static int progressCompletedIterations(TimeSpan elapsed, ItemView item, out int remainingMS)
        {
            //calculate how many complete iterations have passed
            if (item.myprogressvalue + elapsed.TotalMilliseconds < item.mySalaryTimeMS)
            {
                remainingMS = item.mySalaryTimeMS - (item.myprogressvalue + (int)Math.Floor(elapsed.TotalMilliseconds));
                return 0;
            }
            else
            {
                //return will be (int)Math.Floor((elapsed + progressvalue) / saltime)
                //remainingMS will be (elapsed + progressvalue) % saltime
                remainingMS = ((int)elapsed.TotalMilliseconds + item.myprogressvalue) % item.mySalaryTimeMS;
                return (int)Math.Floor((double)((int)elapsed.TotalMilliseconds + item.myprogressvalue) / item.mySalaryTimeMS);
            }
        }
        public static Game SinceYouveBeenGone(Game mygame)
        {
            if (mygame.sinceLastSave.TotalSeconds > 1.0d)
            {
                //myMoney += salary * sincelastsave.TotalSeconds; 
                //thislifetimeMoney += salary * sincelastsave.TotalSeconds;

                double salearned = 0.0d;
                for (int i = 0; i < mygame.myItems.Length; i++)
                {
                    int thisremainingMS;
                    salearned += (progressCompletedIterations(mygame.sinceLastSave, mygame.myItems[i], out thisremainingMS) * mygame.myItems[i].mySalary * mygame.myItems[i].myQty);
                    mygame.myItems[i].myprogressvalue = thisremainingMS;
                }
                mygame.myMoney += salearned;
                mygame.thislifetimeMoney += salearned;

                if (mygame.MySaveType != SaveType.Prestigesave && mygame.MySaveType != SaveType.NewGame)
                {
                    WelcomeBack myWelcome = new WelcomeBack($"Welcome Back!\nYou were gone for {mygame.sinceLastSave.TotalHours:N0} hours, {mygame.sinceLastSave.Minutes:N0} minutes, and {mygame.sinceLastSave.Seconds:N0} seconds.\nYou made ${Stringify((salearned).ToString("R"), StringifyOptions.LongText):N} while you were gone!");
                    myWelcome.ShowDialog();
                }
            }
            return mygame;
        }
        internal static Game ApplyNewPrestige(Game mygame)
        {
            //items and upgrades need to be reset to default
            //clickamount reset to default
            //calculate and apply clickamount and item salaries per new prestigePoints (old + gained)
            //update 'lifetime' variables
            //copy over other persistent variables
            //show popup window/msgbox as modal window

            //*Apply changes to current mygame object, then allow game to continue initialization. Next save will store new info from myGame.

            //Create default game object to initialize items, upgrades, clickamount, etc
            Game tempGame = new Game();
            //copy over relevant vars
            tempGame.FXEnabled = mygame.FXEnabled;
            tempGame.FXVolume = mygame.FXVolume;
            tempGame.MusicEnabled = mygame.MusicEnabled;
            tempGame.MusicVolume = mygame.MusicVolume;
            tempGame.lastlifetimeMoney = mygame.thislifetimeMoney + mygame.lastlifetimeMoney;
            tempGame.matsMinedLifetime = mygame.matsMinedLifetime + mygame.matsMined;
            tempGame.myPurchaseAmount = mygame.myPurchaseAmount;
            tempGame.myWindowState = mygame.myWindowState;
            tempGame.prestigePoints = mygame.prestigePoints + mygame.prestigeGainedNextRestart;
            tempGame.sinceLastSave = mygame.sinceLastSave;  //not sure if we need this yet...
            tempGame.toolTipDelay = mygame.toolTipDelay;
            tempGame.toolTipVisibleTime = mygame.toolTipVisibleTime;
            tempGame.totalGameTime = mygame.totalGameTime;
            tempGame.AutosaveEnabled = mygame.AutosaveEnabled;
            tempGame.AutosaveInterval = mygame.AutosaveInterval;
            tempGame.CurrentLogFile = mygame.CurrentLogFile;

            //calculate and update clickamount and item salaries
            tempGame.clickAmount *= ((tempGame.prestigePoints / (100.0d / tempGame.prestigeMultiplier)) + 1);
            foreach (var item in tempGame.myItems)
            {
                item.mySalary *= ((tempGame.prestigePoints / (100.0d / tempGame.prestigeMultiplier)) + 1);
            }
            PrestigeEarned prestWindow = new PrestigeEarned($"You gained {Stringify(mygame.prestigeGainedNextRestart.ToString("R"), StringifyOptions.LongText):N0} prestige points!");
            prestWindow.ShowDialog();
            return tempGame;
        }

        //----Visual Updates----//
        public void frmMain_UpdateLabels()
        {
            //set main form's labels as needed
            lblMoney.Text = $"Money: ${Stringify(myGame.myMoney.ToString("R"), this.NumberViewSetting):N}";
            lblSalary.Text = $"Salary: ${Stringify(myGame.salary.ToString("R"), this.NumberViewSetting):N} Per Second";
            lblClickAmount.Text = $"Mining: ${Stringify(myGame.clickAmount.ToString("R"), this.NumberViewSetting):N} Per Click";
            lblIncrPerClick.Text = $"Mined Per Click: {myGame.incrperclick:N0}";
            lblMatsMined.Text = $"Materials Mined: {myGame.matsMined:N0}";
            if (myGame.myPurchaseAmount == PurchaseAmount.BuyOne || myGame.myPurchaseAmount == PurchaseAmount.BuyTen || myGame.myPurchaseAmount == PurchaseAmount.Buy100)
            {
                btnPurchAmount.Text = $"Buy: x{(int)myGame.myPurchaseAmount}";
            }
            else if (myGame.myPurchaseAmount == PurchaseAmount.BuyMax)
            {
                btnPurchAmount.Text = "Buy: Max";
            }
            else if (myGame.myPurchaseAmount == PurchaseAmount.BuyNext)
            {
                btnPurchAmount.Text = "Buy: Next";
            }
            bool CanBuyUpgrade = false;
            foreach (UpgradeButton btn in myGame.upgradeButtons)
            {
                if (btn.myUpgrade.Purchased)
                {
                    UpgradeButtonEnable(btn, false);
                }
                //if we can afford it and haven't bought it, enable it and turn it green, if not, disable it and turn it gray. real simple.
                else if (!(btn.myUpgrade.Purchased))
                {
                    //if not purchased
                    if (myGame.myMoney >= btn.myUpgrade.Cost)
                    {
                        //can afford
                        UpgradeButtonEnable(btn, true);
                        CanBuyUpgrade = true;
                    }
                    else
                    {
                        //can't afford
                        UpgradeButtonEnable(btn, false);
                    }
                    btn.Text = btn.myUpgrade.Description + $"\n${Stringify(btn.myUpgrade.Cost.ToString("R"), this.NumberViewSetting):N}";
                }
                
                
            }
            //if any upgrades flagged CanBuyUpgrade, enable btnQuickBuy; otherwise, disable.
            if (CanBuyUpgrade)
            {
                btnQuickBuy.BackColor = Colors.colButtonEnabled;
                btnQuickBuy.ForeColor = Colors.colUpgradeTextEnabled;
                btnQuickBuy.Enabled = true;
            }
            else
            {
                btnQuickBuy.BackColor = Colors.colButtonDisabled;
                btnQuickBuy.ForeColor = Colors.colUpgradeTextDisabled;
                btnQuickBuy.Enabled = false;
            }
        }
        private void timerVisualUpdate_Tick(object sender, EventArgs e)
        {
            foreach (ItemView item in myGame.myItems)
            {
                if (myGame.myPurchaseAmount == PurchaseAmount.BuyMax) { item.purchaseAmount = (int)calcMax(myGame.myMoney, item); }  //only used for max calculation updates
                item.ButtonColor(myGame.myMoney, item.purchaseAmount, item.myCostMult);
                if (item.calculatedCost == 0.00d) { item.calculatedCost = costcalcnew(item, 1); }   //prevent showing $0 in item cost field
                item.UpdateLabels();
            }
            //TODO: Add upgrade labelupdates and buttoncolor updates
            frmMain_UpdateLabels();
            foreach (Label lbl in myGame.floatlabels)
            {
                lbl.Location = new Point(lbl.Location.X, lbl.Location.Y - 40);
            }
            foreach (Label lbl in myGame.floatlabeldeletelist)
            {
                if (myGame.floatlabels.Contains(lbl))
                {
                    myGame.floatlabels.Remove(lbl);
                    lbl.Dispose();
                }
            }
            myGame.floatlabeldeletelist.Clear();

            //debug only
            AutoBuy();
        }
        public void ToggleItemSalaryDisplays()
        {
            //Play click sound
            if (myGame.FXEnabled)
            {
                mciSendString("seek clicksound to start", null, 0, IntPtr.Zero);
                mciSendString("play clicksound", null, 0, IntPtr.Zero);
            }
            //Toggle all item displays
            foreach (ItemView item in myGame.myItems)
            {
                item.displaySalPerSec = !item.displaySalPerSec;
            }
        }
        private void UpgradePanel_Resize(object sender, EventArgs e)
        {
            //calculate and recenter each button within panel
            //do this at frmload as well after buttons are added in
            int truecenterx = UpgradePanel.Location.X + ((UpgradePanel.Width - SystemInformation.VerticalScrollBarWidth) / 2);
            for (int i = 0; i < UpgradePanel.Controls.Count; i++)
            {
                UpgradePanel.Controls[i].Location = new Point((truecenterx - (UpgradePanel.Controls[i].Width / 2)) + UpgradePanel.Margin.Left, UpgradePanel.Controls[i].Location.Y);
            }
            //refresh panel to redraw it and it's controls
            UpgradePanel.Refresh();
        }
        private void itemPanel_Resize(object sender, EventArgs e)
        {
            //override x coordinate in itemPanel to center ItemViews
            for (int i = 0; i < itemPanel.Controls.Count; i++)
            {
                itemPanel.Controls[i].Anchor = AnchorStyles.None;
                //get left x coord of itempanel, add half of itempanel.width to find center of itempanel, then subtract half of item.width to find it's left x coord. Y remains unchanged.
                int xLocation = (itemPanel.Location.X + ((itemPanel.Size.Width - System.Windows.Forms.SystemInformation.VerticalScrollBarWidth) / 2)) - (itemPanel.Controls[i].Size.Width / 2);

                Point newLocation = new Point(xLocation, itemPanel.Controls[i].Location.Y);
                //subtract itemPanel.X from xLocation to prevent adding extra padding due to distance of itemPanel from edge of form.
                Padding padding = new Padding(xLocation - itemPanel.Location.X, 10, 10, 10);
                itemPanel.Controls[i].Margin = padding;
                //itemPanel.Controls[i].Location = newLocation; Not needed afterall, causes flickering due to different locations anyway. Parent Control forces location anyway.
            }
        }
        internal static void UpgradeButtonEnable(UpgradeButton btn, bool enabled)
        {
            if (enabled)    //enabled, we can purchase it
            {
                btn.BackColor = Colors.colButtonEnabled;
                btn.ForeColor = Colors.colUpgradeTextEnabled;
                btn.FlatStyle = FlatStyle.Popup;
                btn.IsEnabled = true;
            }
            else
            {
                if (!btn.myUpgrade.Purchased)   //disabled and we don't own it
                {
                    btn.BackColor = Colors.colButtonDisabled;
                    btn.ForeColor = Colors.colUpgradeTextDisabled;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.IsEnabled = false;
                }
                else    //disabled and we do own it
                {
                    btn.BackColor = Colors.colButtonPurchased;
                    btn.ForeColor = Colors.colUpgradeTextPurchased;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.Text = $"{btn.myUpgrade.Description}\nPurchased!";
                    btn.IsEnabled = false;
                }
            }
        }
        public void SetItemNumView(StringifyOptions newNumSetting)
        {
            foreach (ItemView item in myGame.myItems)
            {
                item.NumViewSetting = newNumSetting;
            }
        }

        //----Save/Load----//
        public void SaveGame()
        {
            myGame.numberviewsetting = this.NumberViewSetting;
            GameState save = new GameState(myGame);

            //if prestige was earned, set flags to ensure recalculation of salaries.
            if (myGame.PrestigeNextRestart)
            {
                save.PrestigeSaveFlag = true;
                save.saveType = SaveType.Prestigesave;
            }
            else
            {
                save.PrestigeSaveFlag = false;
                save.saveType = SaveType.Exitsave;
            }

            save.lastsavetimestamp = DateTime.Now;
            save.SaveLocation = save.SaveLocation == "" ? Environment.CurrentDirectory + @"\GameState.mmf" : save.SaveLocation;
            FileStream fstream = new FileStream(save.SaveLocation, FileMode.Create);

            //serialize and write to disk
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            BinaryFormatter bformatter = new BinaryFormatter();
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            bformatter.Serialize(fstream, save);
            fstream.Dispose();
        }
        public void SaveGame(string saveLocation, bool IsAutosave = false)
        {
            myGame.numberviewsetting = this.NumberViewSetting;
            GameState save = new GameState(myGame);
            save.saveType = myGame.PrestigeNextRestart ? SaveType.Prestigesave : SaveType.Manualsave;
            if (IsAutosave) { save.saveType = SaveType.Autosave; }
            save.lastsavetimestamp = DateTime.Now;
            save.SaveLocation = saveLocation;
            myGame.lastSaveLocation = saveLocation;
            FileStream fstream = new FileStream(saveLocation, FileMode.Create);

            //serialize and write to disk
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            BinaryFormatter bformatter = new BinaryFormatter();
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            bformatter.Serialize(fstream, save);
            fstream.Dispose();
        }
        public GameState? LoadGame()
        {
            FileStreamOptions fsOptions = new FileStreamOptions();
            GameState save;
            fsOptions.Mode = FileMode.Open;
            try
            {
                //create self-disposing stream using default file (can we have this look for any *.mmf file in currentdir? What if there are multiple?)
                using FileStream fstream = new FileStream(Environment.CurrentDirectory + @"\GameState.mmf", fsOptions);
                //create formatter
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                BinaryFormatter bformatter = new BinaryFormatter();
#pragma warning restore SYSLIB0011 // Type or member is obsolete

                save = (GameState)bformatter.Deserialize(fstream);

                fstream.Dispose();

                TimeSpan sincelastsave = DateTime.Now.Subtract(save.lastsavetimestamp);
                save.thislifegametime = save.thislifegametime.Add(sincelastsave);
                save.totalgametime = save.totalgametime.Add(sincelastsave);
                save.SaveLocation = Environment.CurrentDirectory + @"\GameState.mmf";
                return save;
            }
            //If there's a problem or we can't find the file, return null, and the constructor will initialize to default. Next time
            //the game is closed it will save a new file, overwriting the corrupted one if it exists.
            catch (FileNotFoundException noFileEx)
            {

                LogMessage($"Save file not found at {Environment.CurrentDirectory + @"\GameState.mmf"}, initializing default new game...");
                LogMessage($"Exception message: {noFileEx.Message}");
                return null;
            }
            catch (SerializationException binaryEx)
            {

                LogMessage($"Incompatible save - {binaryEx.Message}, initializing defaults");
                return null;
            }
            catch (Exception ex)
            {

                LogMessage($"Unhandled exception occurred during LoadGame() - {ex.Message}, initializing defaults");
                return null;
            }
        }
        public void LoadGame(string loadLocation)
        {
            FileStreamOptions fsOptions = new FileStreamOptions();
            GameState? save;
            try
            {
                fsOptions.Mode = FileMode.Open;
                using FileStream fstream = new FileStream(loadLocation, fsOptions);
                //read from disk and deserialize
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                BinaryFormatter bformatter = new BinaryFormatter();
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                save = (GameState)bformatter.Deserialize(fstream);
                fstream.Dispose();

                if (save != null && (save.saveType == SaveType.Manualsave || save.saveType == SaveType.Exitsave || save.saveType == SaveType.Autosave))
                {
                    TimeSpan sincelastsave = DateTime.Now.Subtract(save.lastsavetimestamp);
                    save.thislifegametime.Add(sincelastsave);
                    save.totalgametime.Add(sincelastsave);
                    string myLog = myGame.CurrentLogFile;
                    myGame = new Game(save);
                    myGame.lastSaveLocation = loadLocation;
                    myGame.CurrentLogFile = myLog;
                    ClearFormItems();
                    CheckForXml();
                    InitAudio();
                    InitControls();
                    LoadItemControlsToForm();
                    LoadUpgradeControlsToForm();
                    frmMain_Load(new object(), EventArgs.Empty);
                }
                else if (save != null && save.saveType == SaveType.Prestigesave)
                {
                    string myLog = myGame.CurrentLogFile;
                    myGame = ApplyNewPrestige(new Game(save));
                    myGame.lastSaveLocation = loadLocation;
                    myGame.CurrentLogFile = myLog;
                    ClearFormItems();
                    CheckForXml();
                    InitAudio();
                    InitControls();
                    LoadItemControlsToForm();
                    LoadUpgradeControlsToForm();
                    frmMain_Load(new object(), EventArgs.Empty);
                }
                else
                {
                    //save==null
                    throw new NotSupportedException();
                }
            }
            catch (NotSupportedException ex)
            {
                LogMessage($"Invalid file loaded from {loadLocation} - {ex.Message}");
                this.WindowState = FormWindowState.Maximized;
                return;
            }
            catch (SerializationException ex)
            {
                LogMessage($"Serialization error in LoadGame({loadLocation}) - {ex.Message} (Save from pre-1101 build?)");
                this.WindowState = FormWindowState.Maximized;
                return;
            }
            catch (Exception ex)
            {
                LogMessage($"Unhandled exception in LoadGame({loadLocation}) - {ex.Message}");
                this.WindowState = FormWindowState.Maximized;
                return;
            }
        }
        public string GetSaveLocation()
        {
            return myGame.lastSaveLocation;
        }
        public void Autosave(object? sender, EventArgs e)
        {
            LogMessage($"Autosaving...");
            SaveGame(myGame.lastSaveLocation, true);
            LogMessage($"Autosave complete!");
            //need some way to show it autosaved...
            PlaySound(SoundList.Ping);
        }
        /// <summary>
        /// Creates a new log file using subsequent naming, and returns the file name as string. Call this as early as you can.
        /// </summary>
        public string CreateLog()
        {
            //check currentdir/logs for *.txt files containing 'GameLog' and get a count of them.
            //create new text file (filestream) called GameLog{existinglogcount + 1}.txt
            //store name of created file in myGame.CurrentLogFile
            if (!Path.Exists(Environment.CurrentDirectory + @"\Logs\"))
            {
                Directory.CreateDirectory(Environment.CurrentDirectory + @"\Logs\");
            }
            string[] allfilenames = Directory.EnumerateFiles(Environment.CurrentDirectory + @"\Logs\").ToArray<string>();
            int logfilecount = allfilenames.Where(x => (x.Contains("GameLog") && x.Contains(".txt"))).Count();
            StreamWriter logstream = File.CreateText(Environment.CurrentDirectory + $@"\Logs\GameLog{logfilecount + 1}.txt");
            logstream.Dispose();
            return Environment.CurrentDirectory + $@"\Logs\GameLog{logfilecount + 1}.txt";

        }
        /// <summary>
        /// Add a line to the currently referenced myGame.CurrentLogFile. Automatically includes DateTime.Now - no need to include that in 'message' parameter.
        /// </summary>
        /// <param name="message">The message to add</param>
        public void LogMessage(string message)
        {
            //open filestream to myGame.CurrentLogFile
            //set filestream position to end of file
            //writeline(DateTime.Now.ToString() + ": " + message)
            //close filestream
            string logFilePath;
            if (myGame.CurrentLogFile == null || myGame.CurrentLogFile == "")
            {
                logFilePath = logfile;
            }
            else { logFilePath = myGame.CurrentLogFile; }
            File.AppendAllLines(logFilePath, new string[] { DateTime.Now + ": " + message });
            //would love to use Async, but need a way to keep them in order and avoid race conditions. Use sync for now.
        }
        internal static void SaveUpgradesToXml(List<Upgrade> Upgrades, string fileName)
        {
            //here we'll save the current upgrade list to disk as an xml file, built by a list of upgradeinfo objects, which are built from 
            //the upgrades in the passed in List<Upgrade> object.
            List<UpgradeInfo> upgradelist = new();
            foreach (Upgrade upgrade in Upgrades)
            {
                UpgradeInfo info = new();
                info.description = upgrade.Description;
                info.itemid = upgrade.itemID;
                info.cost = upgrade.Cost;
                info.multiplier = upgrade.Multiplier;
                info.upgradeid = upgrade.upgradeID;
                info.purchased = upgrade._purchased;
                upgradelist.Add(info);
            }
            XmlSerializer mySer = new(typeof(List<UpgradeInfo>));
            FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate);
            mySer.Serialize(fs, upgradelist);
            fs.Dispose();
            return;
        }
        internal static List<Upgrade> LoadUpgradesFromXml(string fileName)
        {
            //Read file if exists, and build a list of upgrades using UpgradeInfo objects parsed from xml.
            //If there's an issue, return an empty list.
            List<UpgradeInfo>? upgradeinfos = new();
            List<Upgrade> upgradelist = new();
            if (File.Exists(fileName))
            {
                XmlSerializer mySer = new(typeof(List<UpgradeInfo>));
                FileStream fs = new(fileName, FileMode.Open);
                upgradeinfos = (List<UpgradeInfo>?)mySer.Deserialize(fs);
                fs.Dispose();
                if (upgradeinfos != null && upgradeinfos.Count > 0)
                {
                    foreach (UpgradeInfo info in upgradeinfos)
                    {
                        upgradelist.Add(new(info.description, info.cost, info.itemid, info.multiplier, info.upgradeid));
                    }
                    if (upgradelist != null && upgradelist.Count > 0)
                    {
                        return upgradelist;
                    }
                }
            }
            return new();
        }
        internal static List<Upgrade> MergeUpgradesFromXml(List<Upgrade> UpgradeListInternal, List<Upgrade> UpgradeListXml)
        {
            /*-foreach (Upgrade upgradefromxml in UpgradesFromXml)
                -bool UpgradeHandled = false;
                -for (int i = 0; i < upgradelistdefault.Count; i++)
                    -upgradeID is the same
                        -upgradeDefault.Purchased == false
                            -upgradeDefault = upgradeFromXml
                            -UpgradeHandled = true;
                        -upgradeDefault.Purchased == true
                            -Upgrade.Equals==true && desc==desc
                                -UpgradeHandled = true;
                                -break;
                            -Upgrade.Equals==true && desc!=desc
                                -upgradeDefault = upgradeFromXml
                                -upgradeDefault.SetPurchased(true)
                                -UpgradeHandled = true;
                            -Upgrade.Equals!=true
                                -upgradeDefault = upgradeFromXml(purchased defaults to false)
                                -UpgradeHandled = true;
                    -upgradeID is different
                        -break;
                -if (!UpgradeHandled) { UpgradeListDefault.Add(UpgradeFromXml); }*/
            foreach (Upgrade upgradefromxml in UpgradeListXml)
            {
                bool UpgradeHandled = false;
                for (int i = 0; i < UpgradeListInternal.Count; i++)
                {
                    if (upgradefromxml.upgradeID == UpgradeListInternal[i].upgradeID)
                    {
                        if (!UpgradeListInternal[i].Purchased)
                        {
                            UpgradeListInternal[i] = upgradefromxml;
                            UpgradeHandled = true;
                            break;
                        }
                        else
                        {
                            if (UpgradeListInternal[i].Equals(upgradefromxml))
                            {
                                if (UpgradeListInternal[i].Description == upgradefromxml.Description)
                                {
                                    UpgradeHandled = true;
                                    break;
                                }
                                else
                                {
                                    UpgradeListInternal[i] = upgradefromxml;
                                    Upgrade.SetPurchased(UpgradeListInternal[i]);
                                    UpgradeHandled = true;
                                    break;
                                }
                            }
                            else
                            {
                                UpgradeListInternal[i] = upgradefromxml;
                                UpgradeHandled = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                if (!UpgradeHandled)
                {
                    UpgradeListInternal.Add(upgradefromxml);
                }
            }
            return UpgradeListInternal.OrderBy(x => x.Cost).ToList();
        }

        //----Audio Methods----//
        public void SetAudioVolume(int musicVol, int fxVol)
        {
            myGame.MusicVolume = musicVol;
            myGame.FXVolume = fxVol;
            if (musicVol < 0) { musicVol = 0; } else if (musicVol > 1000) { musicVol = 1000; }
            if (fxVol < 0) { fxVol = 0; } else if (fxVol > 1000) { fxVol = 1000; }
            mciSendString($"setaudio backgroundmusic01 volume to {musicVol}", null, 0, IntPtr.Zero);
            mciSendString($"setaudio registersound volume to {fxVol}", null, 0, IntPtr.Zero);
            mciSendString($"setaudio registersound2 volume to {fxVol}", null, 0, IntPtr.Zero);
            mciSendString($"setaudio clicksound volume to {fxVol}", null, 0, IntPtr.Zero);
            mciSendString($"setaudio pingsound volume to {fxVol}", null, 0, IntPtr.Zero);
            for (int i = 1; i <= 8; i++)
            {
                mciSendString($"setaudio pickaxe{i}sound volume to {fxVol}", null, 0, IntPtr.Zero);
            }
        }
        public void ToggleMusic(bool enabled)
        {
            if (enabled)
            {
                if (!myGame.MusicEnabled)
                {
                    //if it wasn't already enabled, then seek to start and play.
                    mciSendString($"seek backgroundmusic01 to start", null, 0, IntPtr.Zero);
                    mciSendString($"play backgroundmusic01 repeat", null, 0, IntPtr.Zero);
                    myGame.MusicEnabled = true;
                }
            }
            else
            {
                if (myGame.MusicEnabled)
                {
                    //if it was enabled, then stop it
                    mciSendString($"stop backgroundmusic01", null, 0, IntPtr.Zero);
                    myGame.MusicEnabled = false;
                }
            }
            LogMessage($"Background Music {(enabled ? "Enabled" : "Disabled")}");
        }
        public void ToggleFX(bool enabled)
        {
            if (enabled)
            {
                if (!myGame.FXEnabled)
                {
                    //if it wasn't already enabled, then set each to on
                    //pickaxe{i}sound   1-8
                    //registersound
                    //registersound2
                    //clicksound
                    mciSendString($"setaudio clicksound on", null, 0, IntPtr.Zero);
                    mciSendString($"setaudio registersound on", null, 0, IntPtr.Zero);
                    mciSendString($"setaudio registersound2 on", null, 0, IntPtr.Zero);
                    mciSendString("setaudio pingsound on", null, 0, IntPtr.Zero);
                    for (int i = 1; i <= 8; i++)
                    {
                        mciSendString($"setaudio pickaxe{i}sound on", null, 0, IntPtr.Zero);
                    }
                    myGame.FXEnabled = true;
                }
            }
            else
            {
                if (myGame.FXEnabled)
                {
                    //if it was enabled, then stop it
                    mciSendString($"setaudio clicksound off", null, 0, IntPtr.Zero);
                    mciSendString($"setaudio registersound off", null, 0, IntPtr.Zero);
                    mciSendString($"setaudio registersound2 off", null, 0, IntPtr.Zero);
                    mciSendString("setaudio pingsound off", null, 0, IntPtr.Zero);
                    for (int i = 1; i <= 8; i++)
                    {
                        mciSendString($"setaudio pickaxe{i}sound off", null, 0, IntPtr.Zero);
                    }
                    myGame.FXEnabled = false;
                }
            }
            LogMessage($"Sound Effects {(enabled ? "Enabled" : "Disabled")}");
        }
        public void PlaySound(SoundList sound)
        {
            int soundCommandResponse = 0;
            switch (sound)
            {
                case (SoundList.ClickSound):
                    {
                        if (myGame.FXEnabled)
                        {
                            mciSendString("seek clicksound to start", null, 0, IntPtr.Zero);
                            soundCommandResponse = mciSendString("play clicksound", null, 0, IntPtr.Zero);
                        }
                        return;
                    }
                case (SoundList.Register):
                    {
                        if (myGame.FXEnabled)
                        {
                            if (myGame.PlayRegisterSound1)
                            {
                                mciSendString("seek registersound to start", null, 0, IntPtr.Zero);
                                soundCommandResponse = mciSendString("play registersound", null, 0, IntPtr.Zero);
                                myGame.PlayRegisterSound1 = false;
                            }
                            else
                            {
                                mciSendString("seek registersound2 to start", null, 0, IntPtr.Zero);
                                soundCommandResponse = mciSendString("play registersound2", null, 0, IntPtr.Zero);
                                myGame.PlayRegisterSound1 = true;
                            }
                        }
                        return;
                    }
                case (SoundList.Pickaxe):
                    {
                        Random randnumber = new Random();
                        int i = randnumber.Next(1, 8);
                        if (myGame.FXEnabled)
                        {
                            mciSendString($"seek pickaxe{i}sound to start", null, 0, IntPtr.Zero);
                            soundCommandResponse = mciSendString($"play pickaxe{i}sound", null, 0, IntPtr.Zero);
                        }
                        return;
                    }
                case (SoundList.Ping):
                    {
                        if (myGame.FXEnabled)
                        {
                            mciSendString("seek pingsound to start", null, 0, IntPtr.Zero);
                            soundCommandResponse = mciSendString("play pingsound", null, 0, IntPtr.Zero);
                        }
                        return;
                    }
            }
            if (soundCommandResponse != 0)
            {
                LogMessage($"PlaySound({sound.ToString()}) Error - MCI Code {soundCommandResponse}");
            }
        }

        private void btnUnlocks_Click(object sender, EventArgs e)
        {
            Unlocks unlocks = new Unlocks(myGame.myItems);
            unlocks.ShowDialog();
        }

        private void pctCenterBackground_Resize(object sender, EventArgs e)
        {
            btnMine.Left = (pctCenterBackground.Width / 2) - (((int)btnMine.Width) / 2);
            btnMine.Top = pctCenterBackground.Height - btnMine.Height - lblMatsMined.Height;
        }

        private void pctCenterBackground_MouseMove(object sender, MouseEventArgs e)
        {
            //the rendering on this was choppy and slow, and produced a lot of artifacts. Might be better off
            //removing the button entirely and moving around a small picturebox, and just detecting clicks on it.
            /*if (Math.Abs(btnMine.Location.X - e.X) > 20 || Math.Abs(btnMine.Location.Y - e.Y) > 20)
            {
                btnMine.Location = PointToClient(new Point(e.X - (btnMine.Width - 30), e.Y - (btnMine.Height - 30)));
            }*/

            
        }

        //----Debug Only----//
        private void AutoBuy()
        {
            if (UseAutobuy)
            {
                //since money calculations are already done inside button methods, this just tries to click all item buy buttons
                //and the quickbuy button once per update. May cause catastrophe, who knows...
                foreach (ItemView item in myGame.myItems)
                {
                    
                    BuyClicked(item);
                }
                btnQuickBuy_Click(this, EventArgs.Empty);
            }
        }
    }

    //------Classes------//
    public struct Game : IEquatable<Game>
    {
        public bool MusicEnabled;
        public bool FXEnabled;
        public bool PlayRegisterSound1;
        internal bool PrestigeNextRestart;
        public int matsMined;
        public int incrperclick;
        public int toolTipDelay;
        public int toolTipVisibleTime;
        public int matsMinedLifetime;
        public int MusicVolume; //0 to 1000
        public int FXVolume; //0 to 1000
        public double myMoney;
        public double salary;
        public double clickAmount;
        public double thislifetimeMoney;
        public double lastlifetimeMoney;
        public double prestigePoints;
        public double prestigeMultiplier;
        public double prestigeGainedNextRestart;
        public TimeSpan thislifeGameTime;
        public TimeSpan totalGameTime;
        public TimeSpan sinceLastSave;
        internal List<Label> floatlabels;
        internal List<Label> floatlabeldeletelist;
        internal List<Upgrade> MainUpgradeList;
        public List<UpgradeButton> upgradeButtons;
        public ItemView[] myItems;
        public PurchaseAmount myPurchaseAmount;
        public System.Windows.Forms.Timer toolTipTimer;
        public Stopwatch GameClock;
        public ToolTip? myTip;
        public FormWindowState myWindowState;
        public string lastSaveLocation;
        public SaveType MySaveType;
        public string CurrentLogFile;
        public bool AutosaveEnabled { get; set; }
        public int AutosaveInterval { get; set; }
        public System.Windows.Forms.Timer AutosaveTimer;
        public StringifyOptions numberviewsetting;

        public const double unlockMultiplier = 2.0d;
        public readonly static int[] unlockList =
            { 1, 10, 25, 50, 100, 200, 300, 400, 500, 600, 666, 700, 777, 800, 900, 1000, 1100, 1111, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000, 2100, 2200, 2222, 2300, 2400, 2500, 2600, 2700, 2800, 2900, 3000, 3100, 3200, 3300, 3333, 3400, 3500, 3600, 3700, 3800, 3900, 4000, 4100, 4200, 4300, 4400, 4500, 4600, 4700, 4800, 4900, 5000 };
        internal readonly static string[] TextStrings = { "Million", "Billion", "Trillion", "Quadrillion", "Quintillion", "Sextillion", "Septillion", "Octillion", "Nonillion", "Decillion", "Undecillion", "Duodecillion", "Tredecillion", "Quattordecillion", "Quindecillion", "Sexdecillion", "Septendecillion", "Octodecillion", "Novemdecillion", "Vigintillion",
                                                "Unvigintillion", "Duovigintillion", "Tresvigintillion", "Quattorvigintillion", "Quinvigintillion", "Sexvigintillion", "Septenvigintillion", "Octovigintillion", "Novemvigintillion", "Trigintillion", "Untrigintillion", "Duotrigintillion", "Tretrigintillion", "Quattortrigintillion", "Quintrigintillion",
                                                "Sextrigintillion", "Septentrigintillion", "Octotrigintillion", "Novemtrigintillion", "Quadragintillion", "Unquadragintillion", "Duoquadragintillion", "Trequadragintillion", "Quattorquadragintillion", "Quinquadragintillion", "Sexquadragintillion", "Septenquadragintillion", "Octoquadragintillion", "Novemquadragintillion",
                                                "Quinquagintillion", "Unquinquagintillion", "Duoquinquagintillion", "Trequinquagintillion", "Quattorquinquagintillion", "Quinquinquagintillion", "Sexquinquagintillion", "Septenquinquagintillion", "Octoquinquagintillion", "Novemquinquagintillion", "Sexagintillion", "Unsexagintillion", "Duosexagintillion",
                                                "Tresexagintillion", "Quattorsexagintillion", "Quinsexagintillion", "Sexsexagintillion", "Septensexagintillion", "Octosexagintillion", "Novemsexagintillion", "Septuagintillion", "Unseptuagintillion", "Duoseptuagintillion", "Treseptuagintillion", "Quattorseptuagintillion", "Quinseptuagintillion", "Sexseptuagintillion",
                                                "Septseptuagintillion", "Octoseptuagintillion", "Novemseptuagintillion", "Octogintillion", "Unoctogintillion", "Duooctogintillion", "Treoctogintillion", "Quattoroctogintillion", "Quinoctogintillion", "Sexoctogintillion", "Septoctogintillion", "Octoctogintillion", "Novemoctogintillion", "Nonagintillion",
                                                "Unnonagintillion", "Duononagintillion", "Trenonagintillion", "Quattornonagintillion", "Quinonagintillion", "Sexnonagintillion", "Septenonagintillion", "Octononagintillion", "Novemnonagintillion", "Centillion", "Uncentillion"};  //handles full size of type 'double'
        internal const double Octoquinquagintillion = 1000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000.00d;

        public bool Equals(Game gameb)
        {
            return (MusicEnabled == gameb.MusicEnabled &&
                FXEnabled == gameb.FXEnabled &&
                PlayRegisterSound1 == gameb.PlayRegisterSound1 &&
                PrestigeNextRestart == gameb.PrestigeNextRestart &&
                matsMined == gameb.matsMined &&
                incrperclick == gameb.incrperclick &&
                toolTipDelay == gameb.toolTipDelay &&
                toolTipVisibleTime == gameb.toolTipVisibleTime &&
                matsMinedLifetime == gameb.matsMinedLifetime &&
                MusicVolume == gameb.MusicVolume &&
                FXVolume == gameb.FXVolume &&
                myMoney == gameb.myMoney &&
                salary == gameb.salary &&
                clickAmount == gameb.clickAmount &&
                thislifetimeMoney == gameb.thislifetimeMoney &&
                lastlifetimeMoney == gameb.lastlifetimeMoney &&
                prestigePoints == gameb.prestigePoints &&
                prestigeMultiplier == gameb.prestigeMultiplier &&
                prestigeGainedNextRestart == gameb.prestigeGainedNextRestart &&
                thislifeGameTime == gameb.thislifeGameTime &&
                totalGameTime == gameb.totalGameTime &&
                sinceLastSave == gameb.sinceLastSave &&
                MainUpgradeList == gameb.MainUpgradeList &&
                upgradeButtons == gameb.upgradeButtons &&
                myItems == gameb.myItems &&
                myPurchaseAmount == gameb.myPurchaseAmount &&
                myWindowState == gameb.myWindowState &&
                lastSaveLocation == gameb.lastSaveLocation &&
                MySaveType == gameb.MySaveType &&
                CurrentLogFile == gameb.CurrentLogFile &&
                AutosaveEnabled == gameb.AutosaveEnabled &&
                AutosaveInterval == gameb.AutosaveInterval);
        }

        /// <summary>
        /// Default constructor - returns a Game object filled with default values, for a new game or new prestige upgrade.
        /// </summary>
        public Game()
        {
            //init myItems Default
            if (this.myItems == default || this.myItems.Length == 0)
            {
                myItems = [new ItemView(1, "Wood Miner", 3.738d, 1.07d, 1.0d, 600, Image.FromFile(Environment.CurrentDirectory + @"\Resources\Icons\wood.png")),
                    new ItemView(2, "Stone Miner", 60d, 1.15d, 60d, 3000, Image.FromFile(Environment.CurrentDirectory + @"\Resources\Icons\granite.png")),
                    new ItemView(3, "Iron Miner", 720d, 1.14d, 540d, 6000, Image.FromFile(Environment.CurrentDirectory + @"\Resources\Icons\pig-iron.png")),
                    new ItemView(4, "Steel Miner", 8640d, 1.13d, 4320d, 12000, Image.FromFile(Environment.CurrentDirectory + @"\Resources\Icons\steel.png")),
                    new ItemView(5, "Diamond Miner", 103680d, 1.12d, 51840d, 24000, Image.FromFile(Environment.CurrentDirectory + @"\Resources\Icons\diamond.png")),
                    new ItemView(6, "Uranium Miner", 1244160d, 1.11d, 622080d, 96000, Image.FromFile(Environment.CurrentDirectory + @"\Resources\Icons\uranium.png")),
                    new ItemView(7, "Antimatter Miner", 14929920d, 1.10d, 7464960d, 384000, Image.FromFile(Environment.CurrentDirectory + @"\Resources\Icons\atom.png")),
                    new ItemView(8, "Black Hole Miner", 179159040d, 1.09d, 89579520d, 1536000, Image.FromFile(Environment.CurrentDirectory + @"\Resources\Icons\black-hole.png"))];
                
            }

            //init Upgrades Default
            //Upgrades are declared as: (string Name, double Cost, int ItemID, double Multiplier, int upgradeID). They can only be created or altered through their constructors.
            //itemID==0 - ClickAmount
            //itemID==1-8(14) - Items
            //itemID==15 - All Items
            //itemID==20 - Prestige Multiplier
            if (this.MainUpgradeList == default)
            {
                MainUpgradeList =
            [
                new Upgrade("Double Tap", 1000.0d, 0, 10, 1), //Click Upgrades
                new Upgrade("Click Amplifier", 25000.0d, 0, 10, 2),
                new Upgrade("Mega-Clicking", 150000.0d, 0, 10, 3),
                new Upgrade("Click Physics", 500000.0d, 0, 10, 4),
                new Upgrade("Parallel Clicking", 1500000.0d, 0, 10, 5),
                new Upgrade("Birch Wood", 250000.0d, 1, 3, 6), //Item1 Upgrades
                new Upgrade("Pine Wood", 20000000000000.0d, 1, 10, 7),   //----This should be more like 10, reevaluate subsequent upgrades
                new Upgrade("Oak Wood", 2000000000000000000.0d, 1, 7, 8),
                new Upgrade("Cherry Wood", 25000000000000000000000.0d, 1, 7, 9),
                new Upgrade("Sequoia Wood", 1000000000000000000000000000.0d, 1, 10, 10),
                new Upgrade("Sandstone", 500000.0d, 2, 3, 11), //Item2 Upgrades
                new Upgrade("Granite", 50000000000000.0d, 2, 3, 12),
                new Upgrade("Limestone", 5000000000000000000.0d, 2, 3, 13),
                new Upgrade("Marble", 50000000000000000000000.0d, 2, 3, 14),
                new Upgrade("Slate", 5000000000000000000000000000.0d, 2, 7, 15),
                new Upgrade("Picked Iron", 1000000.0d, 3, 3, 16), //Item3 Upgrades
                new Upgrade("Cast Iron", 100000000000000.0d, 3, 3, 17),
                new Upgrade("Molded Iron", 7000000000000000000.0d, 3, 3, 18),
                new Upgrade("Forged Iron", 100000000000000000000000.0d, 3, 3, 19),
                new Upgrade("Cold-Fused Iron", 25000000000000000000000000000.0d, 3, 7, 20),
                new Upgrade("Basic Steel", 5000000.0d, 4, 3, 21), //Item4 Upgrades
                new Upgrade("Tempered Steel", 500000000000000.0d, 4, 3, 22),
                new Upgrade("Rolled Steel", 10000000000000000000.0d, 4, 3, 23),
                new Upgrade("Steel Alloy", 200000000000000000000000.0d, 4, 3, 24),
                new Upgrade("Venutian Steel", 100000000000000000000000000000.0d, 4, 7, 25),
                new Upgrade("Flawed Diamond", 10000000.0d, 5, 3, 26), //Item5 Upgrades
                new Upgrade("Improved Diamond", 1000000000000000.0d, 5, 3, 27),
                new Upgrade("Flawless Diamond", 20000000000000000000.0d, 5, 3, 28),
                new Upgrade("Synthetic Diamond", 300000000000000000000000.0d, 5, 3, 29),
                new Upgrade("Quantum Diamond", 250000000000000000000000000000.0d, 5, 7, 30),
                new Upgrade("Waste Uranium", 25000000.0d, 6, 3, 31), //Item6 Upgrades
                new Upgrade("Mined Uranium", 2000000000000000.0d, 6, 3, 32),
                new Upgrade("Refined Uranium", 35000000000000000000.0d, 6, 3, 33),
                new Upgrade("Synthetic Uranium", 400000000000000000000000.0d, 6, 3, 34),
                new Upgrade("Quantum Uranium", 500000000000000000000000000000.0d, 6, 7, 35),
                new Upgrade("Low Yield Antimatter", 500000000.0d, 7, 3, 36), //Item7 Upgrades
                new Upgrade("Mid Yield Antimatter", 5000000000000000.0d, 7, 3, 37),
                new Upgrade("High Yield Antimatter", 50000000000000000000.0d, 7, 3, 38),
                new Upgrade("Perfect Antimatter", 400000000000000000000000.0d, 7, 3, 39),
                new Upgrade("CPT Reversed Antimatter", 1000000000000000000000000000000.0d, 7, 7, 40),
                new Upgrade("Plank Black Hole", 10000000000.0d, 8, 3, 41), //Item8 Upgrades
                new Upgrade("Primordial Black Hole", 7000000000000000.0d, 8, 3, 42),
                new Upgrade("Rogue Black Hole", 75000000000000000000.0d, 8, 3, 43),
                new Upgrade("Supermassive Black Hole", 600000000000000000000000.0d, 8, 7, 44),
                new Upgrade("Universal Black Hole", 5000000000000000000000000000000.0d, 8, 7, 45),
                new Upgrade("Tax Adjustment", 1000000000000.0d, 15, 3, 46), //All-Item Upgrades
                new Upgrade("Ledger Spoofing", 50000000000000000.0d, 15, 3, 47),
                new Upgrade("Illegal Workers", 500000000000000000000.0d, 15, 3, 48),
                new Upgrade("Off-Shore Mining", 900000000000000000000000.0d, 15, 3, 49),
                new Upgrade("Cult Following", 1000000000000000000000000000000000.0d, 15, 7, 50),
                new Upgrade("Material Lobbying", 100000000000000000.0d, 20, 1.01, 51), //Prestige Upgrades
                new Upgrade("Investor Fraud", 1000000000000000000000.0d, 20, 1.01, 52),
                new Upgrade("Controlled Striking", 10000000000000000000000000.0d, 20, 1.02, 53),
                new Upgrade("Space Investing", 1000000000000000000000000000000000000.0d, 20, 1.05, 54),
                new Upgrade("Planetary Ransom", 1000000000000000000000000000000000000000.0d, 20, 1.06, 55)
                ];
                //Upgrades are declared as: (string Name, double Cost, int ItemID, double Multiplier, int upgradeID)
                MainUpgradeList = (List<Upgrade>)MainUpgradeList.OrderBy(x => x.Cost).ToList();
            }
            if (this.upgradeButtons == default) { upgradeButtons = new List<UpgradeButton>(); }
            if (this.toolTipVisibleTime == default) { toolTipVisibleTime = 2500; }  //How long will tooltips stay visible?
            if (this.toolTipDelay == default) { toolTipDelay = 500; }   //How long do we have to hover before tooltips pop up?
            if (this.prestigeMultiplier == default) { prestigeMultiplier = 2.0d; }
            if (this.clickAmount == default) { clickAmount = 0.25; }
            if (this.salary == default) { salary = 0.0d; }  //Deprecated - Phasing out
            if (this.matsMined == default) { matsMined = 0; }
            if (this.myMoney == default) { myMoney = 0.0d; }
            if (this.incrperclick == default) { incrperclick = 1; }
            if (this.myPurchaseAmount == default) { myPurchaseAmount = PurchaseAmount.BuyOne; }
            if (this.thislifetimeMoney == default) { this.thislifetimeMoney = 0.0d; }   //if they're default here, then we couldn't load from the last save. Set to 0.
            if (this.prestigePoints == default) { this.prestigePoints = 0.0d; }
            if (this.lastlifetimeMoney == default) { this.lastlifetimeMoney = 0.0d; }
            if (this.floatlabels == default) { floatlabels = new List<Label>(); }
            if (this.floatlabeldeletelist == default) { floatlabeldeletelist = new List<Label>(); }
            if (this.toolTipTimer == default) { toolTipTimer = new System.Windows.Forms.Timer(); }
            if (this.GameClock == default) { GameClock = new Stopwatch(); }
            if (this.matsMinedLifetime == default) { matsMinedLifetime = 0; }
            if (this.thislifeGameTime == default) { thislifeGameTime = TimeSpan.Zero; }
            if (this.totalGameTime == default) { totalGameTime = TimeSpan.Zero; }
            if (this.sinceLastSave == default) { sinceLastSave = TimeSpan.Zero; }
            this.PlayRegisterSound1 = true;
            this.MusicEnabled = true;
            this.FXEnabled = true;
            if (this.MusicVolume == default) { this.MusicVolume = 500; }
            if (this.FXVolume == default) { this.FXVolume = 500; }
            this.PrestigeNextRestart = false;
            this.lastSaveLocation = Environment.CurrentDirectory + @"\GameState.mmf";
            this.MySaveType = SaveType.NewGame;
            this.AutosaveEnabled = false;
            this.AutosaveInterval = 0;
            this.AutosaveTimer = new();
            this.CurrentLogFile = "";
            this.numberviewsetting = StringifyOptions.LongText;
        }
        /// <summary>
        /// Game Load constructor - creates a new Game object from GameState (save) data.
        /// </summary>
        /// <param name="save"></param>
        public Game(GameState save)
        {
            MusicEnabled = save.MusicEn;
            FXEnabled = save.FXEn;
            PlayRegisterSound1 = false;  //simply an audio toggle, setting it here for explicit declaration purposes
            PrestigeNextRestart = save.PrestigeSaveFlag;
            matsMined = save.matsMined;
            incrperclick = save.incrperclick;
            toolTipDelay = save.toolTipDelay;
            toolTipVisibleTime = save.toolTipVisibleTime;
            matsMinedLifetime = save.matsMinedLifetime;
            MusicVolume = save.MusicVol;
            FXVolume = save.FXVol;
            myMoney = save.myMoney;
            salary = save.salary;
            clickAmount = save.clickAmount;
            thislifetimeMoney = save.thislifetimeMoney;
            lastlifetimeMoney = save.lastlifetimeMoney;
            prestigePoints = save.prestigePoints;
            prestigeMultiplier = save.prestigeMultiplier;
            prestigeGainedNextRestart = save.PrestigePointsGained;
            thislifeGameTime = save.thislifegametime;
            totalGameTime = save.totalgametime;
            floatlabels = new List<Label>();
            floatlabeldeletelist = new List<Label>();
            MainUpgradeList = save.MainUpgradeList;
            upgradeButtons = new List<UpgradeButton>();
            myItems = new ItemView[save.myItemDatas.Length];
            for (int i = 0; i < myItems.Length; i++)
            {
                myItems[i] = new ItemView(save.myItemDatas[i]);
            }
            myPurchaseAmount = save.myPurchaseAmount;
            toolTipTimer = new();
            GameClock = new();
            myTip = new ToolTip();
            myWindowState = save.lastWindowState;
            sinceLastSave = DateTime.Now - save.lastsavetimestamp;
            lastSaveLocation = save.SaveLocation;
            MySaveType = save.saveType;
            AutosaveEnabled = save.AutosaveEnabled;
            AutosaveInterval = save.AutosaveInterval;
            AutosaveTimer = new();
            CurrentLogFile = save.CurrentLogFile == "" ? "" : save.CurrentLogFile;
            numberviewsetting = save.numberviewsetting;
        }
    }
    [Serializable]
    public class GameState
    {
        internal ItemData[] myItemDatas;    //Serializable object for ItemView
        internal List<Upgrade> MainUpgradeList;//
        internal int toolTipVisibleTime;//
        internal int toolTipDelay;//
        internal double prestigeMultiplier;//
        internal double clickAmount;//
        internal double salary;//
        internal int matsMined;//
        internal double myMoney;//
        internal int incrperclick;//
        internal PurchaseAmount myPurchaseAmount;//
        internal double thislifetimeMoney;//
        internal double prestigePoints;//
        internal double lastlifetimeMoney;//
        internal TimeSpan totalgametime;//
        internal TimeSpan thislifegametime;//
        internal DateTime lastsavetimestamp;//--
        internal FormWindowState lastWindowState;//--
        internal bool PrestigeSaveFlag;//nextrestart//--
        internal double PrestigePointsGained;//--
        internal int MusicVol;//
        internal int FXVol;//
        [OptionalField(VersionAdded = 2)]
        internal bool MusicEn;//
        [OptionalField(VersionAdded = 2)]
        internal bool FXEn;//
        [OptionalField(VersionAdded = 2)]
        internal int matsMinedLifetime;//
        [OptionalField(VersionAdded = 3)]
        internal SaveType saveType;
        [OptionalField(VersionAdded = 3)]
        internal string SaveLocation;
        [OptionalField(VersionAdded = 3)]
        internal bool AutosaveEnabled;
        [OptionalField(VersionAdded = 3)]
        internal int AutosaveInterval;
        [OptionalField(VersionAdded = 3)]
        internal string CurrentLogFile;
        [OptionalField(VersionAdded = 4)]
        internal StringifyOptions numberviewsetting;
        
        /// <summary>
        /// Creates a GameState (a save structure) for Game object. 
        /// </summary>
        /// <param name="game">Required - the Game object from which to get data to save.</param>
        public GameState(Game game)
        {
            myItemDatas = new ItemData[game.myItems.Length];
            for (int i = 0; i < myItemDatas.Length; i++)
            {
                myItemDatas[i] = new ItemData(game.myItems[i]);
            }
            MainUpgradeList = game.MainUpgradeList;
            toolTipVisibleTime = game.toolTipVisibleTime;
            toolTipDelay = game.toolTipDelay;
            prestigeMultiplier = game.prestigeMultiplier;
            clickAmount = game.clickAmount;
            salary = game.salary;
            matsMined = game.matsMined;
            matsMinedLifetime = game.matsMinedLifetime;
            myMoney = game.myMoney;
            incrperclick = game.incrperclick;
            myPurchaseAmount = game.myPurchaseAmount;
            thislifetimeMoney = game.thislifetimeMoney;
            prestigePoints = game.prestigePoints;
            lastlifetimeMoney = game.lastlifetimeMoney;
            totalgametime = game.totalGameTime;
            thislifegametime = game.thislifeGameTime;
            lastsavetimestamp = DateTime.Now;
            lastWindowState = game.myWindowState;
            PrestigeSaveFlag = game.PrestigeNextRestart;
            PrestigePointsGained = game.prestigeGainedNextRestart;
            MusicVol = game.MusicVolume;
            FXVol = game.FXVolume;
            MusicEn = game.MusicEnabled;
            FXEn = game.FXEnabled;
            saveType = game.PrestigeNextRestart ? SaveType.Prestigesave : game.MySaveType;
            SaveLocation = game.lastSaveLocation;
            AutosaveEnabled = game.AutosaveEnabled;
            AutosaveInterval = game.AutosaveInterval;
            CurrentLogFile = this.saveType == SaveType.Prestigesave ? game.CurrentLogFile : "";
            numberviewsetting = game.numberviewsetting;
        }
        
    }
    [Serializable]
    internal struct Upgrade(string description, double cost, int ID, double multiplier, int upgradeID) : IEquatable<Upgrade>
    {
        //public getters and struct declaration means we can read the struct and it's properties from anywhere, even a different namespace.
        //private fields and internal constructor mean that only the struct can access it's fields, and only this assembly can create new upgrades.
        public string Description { get { return _description; } }
        public double Cost { get { return _cost; } }
        public int itemID { get { return _itemID; } }
        public int upgradeID { get { return _upgradeID; } }
        public double Multiplier { get { return _multiplier; } }
        public bool Purchased { get { return _purchased; } }

        private string _description = description;
        private double _cost = cost;
        private int _itemID = ID;
        private int _upgradeID = upgradeID;
        private double _multiplier = multiplier;
        public bool _purchased;

        internal static Upgrade SetPurchased(Upgrade myUpgrade)
        {
            myUpgrade._purchased = true;
            return myUpgrade;
        }
        internal static Upgrade UnsetPurchased(Upgrade myUpgrade)
        {
            myUpgrade._purchased = false;
            return myUpgrade;
        }

        public bool Equals(Upgrade upgrade)
        {
            return (this.upgradeID == upgrade.upgradeID && this.Cost == upgrade.Cost && this.itemID == upgrade.itemID && this.Multiplier == upgrade.Multiplier);
        }
        public override bool Equals(object? obj)
        {
            return (obj is Upgrade upgr && Equals(upgr));
        }
        public override int GetHashCode()
        {
            return this.upgradeID.GetHashCode() + this.Cost.GetHashCode() + this.itemID.GetHashCode() + this.Multiplier.GetHashCode();
        }

    }
    public struct UpgradeInfo
    {
        public string description;
        public int itemid;
        public int upgradeid;
        public double cost;
        public double multiplier;
        public bool purchased;
    }
    public struct ItemInfo
    {
        public string name;
        public int itemid;
        public double cost;
        public double costmultiplier;
        public double salary;
        public int timeinms;
    }
    public class UpgradeButton : Button
    {
        internal Upgrade myUpgrade { get; set; }
        /// <summary>
        /// Custom property used instead of default enable
        /// </summary>
        internal bool IsEnabled { get; set; }
    }
    [Serializable]
    public static class Colors
    {
        public static Color colPrimary = Color.Green;
        public static Color colSecondary = Color.SandyBrown;
        public static Color colTertiary = Color.Tan;
        public static Color colDisable = Color.Gray;
        public static Color colButtonDisabled = Color.FromArgb(51, 46, 60);//RaisinBlack
        public static Color colButtonEnabled = Color.FromArgb(63, 210, 255);//PaleAzure
        public static Color colButtonPurchased = Color.ForestGreen;
        public static Color colBackground = Color.FromArgb(210, 180, 140);//Tan
        public static Color colTextPrimary = Color.Black;
        public static Color colTextSecondary = Color.White;
        public static Color colBorders = Color.FromArgb(78, 213, 215);//TiffanyBlue
        public static Color colUpgradeTextEnabled = Color.FromArgb(0, 29, 107);
        public static Color colUpgradeTextDisabled = Color.FromArgb(188, 194, 174);
        public static Color colUpgradeTextPurchased = Color.FromArgb(255, 255, 255);
    }
    /// <summary>
    /// Deprecated - DO NOT USE. Here for compatibility with v1.1.0.1-alpha saves! Reference static class Colors instead.
    /// </summary>
    [Serializable]
    public class MyColors   //Deprecated - Here for compatibility
    {
        public Color colPrimary = Color.Green;
        public Color colSecondary = Color.SandyBrown;
        public Color colTertiary = Color.Tan;
        public Color colDisable = Color.Gray;
        public Color colButtonDisabled = Color.FromArgb(37, 39, 46);//BlackOlive
        public Color colButtonEnabled = Color.FromArgb(63, 210, 255);//PaleAzure
        public Color colButtonPurchased = Color.FromArgb(20, 81, 195);//SteelBlue
        public Color colBackground = Color.FromArgb(210, 180, 140);//Tan
        public Color colTextPrimary = Color.Black;
        public Color colTextSecondary = Color.White;
        public Color colBorders = Color.FromArgb(78, 213, 215);//TiffanyBlue
        [OptionalField(VersionAdded = 3)]
        public Color colUpgradeTextEnabled = Color.FromArgb(0, 29, 107);
        [OptionalField(VersionAdded = 3)]
        public Color colUpgradeTextDisabled = Color.FromArgb(188, 194, 174);
        [OptionalField(VersionAdded = 3)]
        public Color colUpgradeTextPurchased = Color.FromArgb(255, 255, 255);
    }

    //------Enums------//
    /// <summary>
    /// Represents available purchase amounts using button switch. Default value is BuyOne.
    /// </summary>
    [DefaultValue(PurchaseAmount.BuyOne)]
    public enum PurchaseAmount
    {
        BuyOne = 1,
        BuyTen = 10,
        Buy100 = 100,
        BuyNext = 200,
        BuyMax = -1
    }
    public enum SoundList
    {
        ClickSound = 1,
        Pickaxe = 2,
        Register = 4,
        Ping = 8
    }
    [DefaultValue(SaveType.Exitsave)]
    public enum SaveType
    {
        Exitsave = 10,
        Manualsave = 20,
        Prestigesave = 40,
        NewGame = 80,
        Autosave = 160
    }
    
    
}
