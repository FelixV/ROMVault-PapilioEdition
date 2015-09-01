using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ROMVault2.Properties;
using ROMVault2.RvDB;
using ROMVault2.Utils;
using ROMVault2.SupportedFiles;

namespace ROMVault2
{
    public partial class FrmMain : Form
    {

        /* autodetect A2601 code ported from Stella */
        public string autodetectA2601(RvDir tGame)
        {

            string CWD = string.Concat(Application.StartupPath, "\\papilio\\_tmp\\");
            string cartrom = RomGrid.Rows[0].Cells["CRom"].Value.ToString();
            if (System.IO.File.Exists(string.Concat(CWD,cartrom)))
            {
                string[] cartrom_size = RomGrid.Rows[0].Cells["CSize"].Value.ToString().Trim().Split(' ');

                //MessageBox.Show(cartrom_size[0].ToString());
                int rom_size = Convert.ToInt32(cartrom_size[0]);
                string cartrom_data = BitConverter.ToString(File.ReadAllBytes(string.Concat(CWD, cartrom)));

                // missing a ton of bank types..

                if (rom_size <= 2048)
                {
                    return "2K";
                }

                if (rom_size <= 4096)
                {
                    return "4K";
                }

                if (rom_size <= 8 * 1024)
                {
                    bool F8 = isProbablyF8(cartrom_data);

                    if (isProbablySC(cartrom_data))
                        return "F8-SC";

                    if (isProbablyE0(cartrom_data))
                        return "E0";

                    if (isProbably3F(cartrom_data))
                        return "3F";

                    if (isProbablyFE(cartrom_data) && !F8)
                        return "FE";

                    //if (isProbablyF8(cartrom_data))
                        return "F8";
                }

                if (rom_size <= 16 * 1024)
                {
                    if (isProbablySC(cartrom_data))
                        return "F6-SC";

                    //if (isProbablyF6(cartrom_data))
                        return "F6";
                }

                if (rom_size <= 32 * 1024)
                {
                    //if (isProbably3F(cartrom_data))
                        return "3F";
                }

                if (rom_size <= 64 * 1024)
                {
                    //if (isProbably3F(cartrom_data))
                        return "3F";
                }

                if (rom_size <= 128 * 1024)
                {
                    //if (isProbably3F(cartrom_data))
                        return "3F";
                }

                if (rom_size <= 256 * 1024)
                {
                    //if (isProbably3F(cartrom_data))
                        return "3F";
                }

                if (isProbably3F(cartrom_data))
                    return "3F";

                doLog(" - - Unsupported BSS Type. Trying No BSS (4K) method");
                return "4K";

            } else {
                doLog(string.Concat(" - - Aborting - `",cartrom,"` not found"));
                return "-1";
            }

            //return "-1";

        }

        // adapted from stackoverflow.com by rjdevereux
        public bool searchForBytes(string cartrom_data, string signature, int numTimes)
        {
            int cartrom_data_Length = cartrom_data.Length;
            cartrom_data.Replace(signature, "");
            int times = (cartrom_data_Length - cartrom_data.Length) / signature.Length;
            if (times >= numTimes)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }

        public bool isProbably3F(string cartrom_data)
        {
            // look for 0x85; 0x3F   x2 or more times
            return searchForBytes(cartrom_data, "85-3F",2); 
        }

        public bool isProbablyE0(string cartrom_data)
        {
            string[] signatures = { "8D-E0-1F", "8D-E0-5F", "8D-E9-FF", "0C-E0-1F", "AD-E0-1F", "AD-E9-FF", "AD-ED-FF", "AD-F3-BF" };
            foreach (string signature in signatures)
            {
                if (searchForBytes(cartrom_data, signature,1))
                {
                    return true;
                }
            }
            return false;
        }

        public bool isProbablyF6(string cartrom_data)
        {
            return false; // PLACEHOLDER
        }

        public bool isProbablyF8(string cartrom_data)
        {
            txtDebug.AppendText("\r\nF8 Detect debug\r\n" + cartrom_data + "\r\n");
            return searchForBytes(cartrom_data, "8D-F9-1F", 2);
        }

        public bool isProbablySC(string cartrom_data)
        {
            
            string signature = "";
            for (int i = 1; i <= 256; i++)
                signature = signature + "X-";

            signature = signature.Replace("X", cartrom_data.Substring(0, 2));
                        
            txtDebug.AppendText("\r\nSC Detect debug\r\nsig\r\n" + signature + "\r\ndata\r\n" + cartrom_data + "\r\n");

            if (cartrom_data.StartsWith(signature))
                return true;

            return false;

        }

        public bool isProbablyFE(string cartrom_data)
        {
            string[] signatures = { "20-00-D0-C6-C5", "20-C3-F8-A5-82", "D0-FB-20-73-FE", "20-00-F0-84-D6" };
            foreach (string signature in signatures)
            {
                if (searchForBytes(cartrom_data, signature, 1))
                {
                    return true;
                }
            }

            return false;
        }

        public bool isProbablyNoBank(string cartrom_data)
        {
            return false; // placeholder .. add to autodetect
        }

        /*
        public bool isProbably(string cartrom_data)
        {
            return false; // placeholder .. add to autodetect
        }
        */







    }
}
