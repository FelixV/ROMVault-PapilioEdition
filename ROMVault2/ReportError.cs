﻿/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using System.Windows.Forms;
using ROMVault2.RvDB;
using ROMVault2.Utils;


namespace ROMVault2
{
    public static class ReportError
    {

        public static void UnhandledExceptionHandler(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            try
            {
                // Create Error Message
                string message = string.Format("An Application Error has occurred.\r\n\r\nEXCEPTION:\r\nSource: {0}\r\nMessage: {1}\r\n", e.Exception.Source, e.Exception.Message);
                if (e.Exception.InnerException != null)
                {
                    message += string.Format("\r\nINNER EXCEPTION:\r\nSource: {0}\r\nMessage: {1}\r\n", e.Exception.InnerException.Source, e.Exception.InnerException.Message);
                }
                message += string.Format("\r\nSTACK TRACE:\r\n{0}", e.Exception.StackTrace);

                FrmShowError fshow = new FrmShowError();
                fshow.settype(message);
                fshow.ShowDialog();

                Environment.Exit(0);
            }
            catch
            {
                Environment.Exit(0);
            }
        }

        public static void UnhandledExceptionHandler(Exception e)
        {
            try
            {
                // Create Error Message
                string message = string.Format("An Application Error has occurred.\r\n\r\nEXCEPTION:\r\nSource: {0}\r\nMessage: {1}\r\n", e.Source, e.Message);
                if (e.InnerException != null)
                {
                    message += string.Format("\r\nINNER EXCEPTION:\r\nSource: {0}\r\nMessage: {1}\r\n", e.InnerException.Source, e.InnerException.Message);
                }
                message += string.Format("\r\nSTACK TRACE:\r\n{0}", e.StackTrace);

                FrmShowError fshow = new FrmShowError();
                fshow.settype(message);
                fshow.ShowDialog();

            }
            catch
            {
            }
        }

        public static void UnhandledExceptionHandler(string e1)
        {
            try
            {
                // Create Error Message
                string message = string.Format("An Application Error has occurred.\r\n\r\nEXCEPTION:\r\nMessage:");
                message += e1 + "\r\n";

                message += string.Format("\r\nSTACK TRACE:\r\n{0}", Environment.StackTrace);

                FrmShowError fshow = new FrmShowError();
                fshow.settype(message);
                fshow.ShowDialog();

                Environment.Exit(0);
            }
            catch
            {
                Environment.Exit(0);
            }
        }


        //public static void SendAndShowDat(string message, string filename)
        //{
        //    SendErrorMessageDat(message, filename);
        //    Show(message, "ROMVault - Dat Reading Error");
        //}

        public static void SendAndShow(string message, string caption = "ROMVault", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Exclamation)
        {
            Show(message, caption, buttons, icon);
        }

        public static void Show(string text, string caption = "ROMVault", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Exclamation)
        {
            if (Program.SyncCont != null)
                Program.SyncCont.Send(callback => MessageBox.Show(text, caption, buttons, icon), null);
            else
                MessageBox.Show(text, caption, buttons, icon);
        }

        private static string _logfilename;

        public static void ReportList(List<RvFile> files)
        {
            if (!Settings.DebugLogsEnabled) return;

            string dir, now;
            OpenLogFile(out dir, out now);
            dir = Path.Combine(dir, now + " DataBaseLog.txt");
            TextWriter sw = new StreamWriter(dir, false);
            for (int i = 0; i < files.Count; i++)
            {
                RvFile f = files[i];
                f.ReportIndex = i;
                ReportFile(sw, f);
            }

            sw.Flush();
            sw.Close();
        }

        private static void OpenLogFile(out string dir, out string now)
        {
            dir = Path.Combine(Application.StartupPath, "Logs");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            now = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            now = now.Replace("\\", "-");
            now = now.Replace("/", "-");
            now = now.Replace(":", "-");

            _logfilename = Path.Combine(dir, now + " UpdateLog.txt");

        }

        private static void ReportFile(TextWriter sw, RvFile f)
        {
            sw.WriteLine(f.ReportIndex.ToString("D8") + " " + ArrByte.ToString(f.CRC) + " " + f.GotStatus.ToString().PadRight(10) + " " + f.RepStatus.ToString().PadRight(15) + " " + f.TreeFullName);
        }

        public static void LogOut(string s)
        {
            if (!Settings.DebugLogsEnabled) return;

            if (_logfilename == null)
            {
                string dir, now;
                OpenLogFile(out dir, out now);
            }

            StreamWriter sw = new StreamWriter(_logfilename, true);
            sw.WriteLine(s);
            sw.Flush();
            sw.Close();

        }
        public static void LogOut(RvFile f)
        {
            if (!Settings.DebugLogsEnabled) return;

            StreamWriter sw = new StreamWriter(_logfilename, true);
            ReportFile(sw, f);
            sw.Flush();
            sw.Close();
        }
    }
}
