﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Wokhan.WindowsFirewallNotifier.Common.Helpers;
using Wokhan.WindowsFirewallNotifier.Common.Properties;

namespace Wokhan.WindowsFirewallNotifier.RuleManager
{
    class Program
    {
        private static List<string> tmpnames;

        [STAThread]
        static void Main(string[] args)
        {
            LogHelper.Debug("Starting RuleManager: " + Environment.CommandLine);
            try
            {
                if (args.Count() == 0)
                {
                    MessageBox.Show(Resources.MSG_RULEMANAGER_ARGUMENTS_ERR, Resources.MSG_DLG_ERR_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }
                if (args.Count() != 1)
                {
                    throw new ArgumentException("Wrong number of arguments!");
                }
                string[] param = Encoding.Unicode.GetString(Convert.FromBase64String(args[0])).Split(new string[] { "#$#" }, StringSplitOptions.None);

                if (param.Count() != 11)
                {
                    throw new ArgumentException("Invalid arguments!");
                }

                string rname = param[0];
                string path = param[1];
                string appPkgId = param[2];
                string localUserOwner = param[3];
                string sv = param[4];
                string[] services = (sv != null ? sv.Split(',') : new string[] { });
                int protocol = int.Parse(param[5]);
                string target = param[6];
                string targetPort = param[7];
                string localPort = param[8];
                int profile = int.Parse(param[9]);
                string action = param[10];
                bool keepOpen = false;
                bool ret = true;

                switch (action)
                {
                    case "A":
                    case "B":
                        foreach (var service in services)
                        {
                            FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(rname + (service != null ? "[" + service + "]" : ""), path, appPkgId, localUserOwner, service, protocol, target, targetPort, localPort, profile, action);
                            ret = ret && newRule.Apply(false);
                        }
                        break;

                    case "T":
                        tmpnames = new List<string>();
                        foreach (var service in services)
                        {
                            string tmpRuleName = Common.Properties.Resources.RULE_TEMP_PREFIX + " " + Guid.NewGuid().ToString();
                            tmpnames.Add(tmpRuleName);
                            FirewallHelper.CustomRule newRule = new FirewallHelper.CustomRule(tmpRuleName, path, appPkgId, localUserOwner, service, protocol, target, targetPort, localPort, profile, "A"); //FIXME: Hardcoded action!
                            ret = ret && newRule.Apply(true);
                        }
                        keepOpen = true;
                        break;

                    default:
                        throw new Exception("Unknown action type: " + action.ToString());
                }

                if (!ret)
                {
                    throw new Exception("Unable to create the rule");
                }
                else if (keepOpen)
                {
                    NotifyIcon ni = new NotifyIcon();
                    ni.Click += new EventHandler(ni_Click);
                    ni.BalloonTipIcon = ToolTipIcon.Info;
                    ni.BalloonTipTitle = Resources.RULE_TEMP_TITLE;
                    ni.BalloonTipText = String.Format(Resources.RULE_TEMP_DESCRIPTION, path);
                    ni.Icon = new Icon(SystemIcons.Shield, new Size(16, 16));
                    ni.Visible = true;
                    ni.ShowBalloonTip(2000);

                    Application.Run();
                }
            }
            catch (Exception e)
            {
                LogHelper.Error("WFNRuleManager failure", e);
                Environment.Exit(1);
            }

            Environment.Exit(0);
        }

        private static void ni_Click(object sender, EventArgs e)
        {
            LogHelper.Info("Now going to remove temporary rule(s)...");

            if (!tmpnames.All(kv => FirewallHelper.RemoveRule(kv)))
            {
                MessageBox.Show(Resources.MSG_RULE_RM_FAILED, Resources.MSG_DLG_ERR_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            Environment.Exit(0);
        }

    }
}
