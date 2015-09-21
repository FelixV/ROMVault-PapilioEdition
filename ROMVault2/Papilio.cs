/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2010                                 *
 ******************************************************/
using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ROMVault2.Properties;
using ROMVault2.RvDB;
using ROMVault2.Utils;
using ROMVault2.SupportedFiles;



// Icons by http://www.iconarchive.com/show/blankon-icons-by-zeusbox.html


namespace ROMVault2
{
    public partial class FrmMain : Form
    {

        private void resortGameGrid(int colIndex=2)
        {

            //MessageBox.Show("resortGameGrid() called");

            // only allow sort on CGame/CDescription
            if (colIndex != 1 && colIndex != 2)
                return;

            DataGridViewColumn newColumn = GameGrid.Columns[colIndex];
            DataGridViewColumn oldColumn = GameGrid.Columns[_gameGridSortColumnIndex];
            _gameGridSortOrder = SortOrder.Ascending;

            GameGrid.Sort(new GameGridRowComparer(_gameGridSortOrder, colIndex));
            newColumn.HeaderCell.SortGlyphDirection = _gameGridSortOrder;
            _gameGridSortColumnIndex = colIndex;

        }

        private void doLog(string logEvent)
        {
            lstLogs.Items.Add(logEvent);
            txtDebug.AppendText(logEvent);
            txtDebug.AppendText("\r\n");
            lstLogs.TopIndex = lstLogs.Items.Count - 1;
            lstLogs.Refresh();
            return;
        }

        void CopyStream(Stream destination, Stream source)
        {
            int count;
            byte[] buffer = new byte[1024];
            while ((count = source.Read(buffer, 0, buffer.Length)) > 0)
                destination.Write(buffer, 0, count);
        }

        // mode 1: stdout; mode 2: stderr; mode 3: stdout\r\n%SPLITTER%\r\nstderr
        public string RunEXE(string CWD, string exeName, string arguments, int returnMode, bool debugMode)
        {

            if (returnMode >= 1 && returnMode <= 3)
            {
                // noop
            }
            else
            {
                // invalid mode! (should never see this)
                return "INVALID MODE\r\n%SPLITTER%\r\nINVALID MODE";
            }

            if (debugMode == true)
            {
                doLog(" - RunEXE - ");
                doLog(" - - EXE:" + exeName);
                doLog(" - - ARG:" + arguments);
            }

            Process exeProcess = new Process();

            exeProcess.StartInfo.FileName = string.Concat(CWD, exeName);
            exeProcess.StartInfo.Arguments = arguments;
            exeProcess.StartInfo.UseShellExecute = false;
            exeProcess.StartInfo.CreateNoWindow = true;
            exeProcess.StartInfo.WorkingDirectory = CWD;
            exeProcess.StartInfo.RedirectStandardOutput = true;
            exeProcess.StartInfo.RedirectStandardError = true;

            exeProcess.Start();

            string exeOutput = exeProcess.StandardOutput.ReadToEnd();
            string exeOutputError = exeProcess.StandardError.ReadToEnd();

            exeProcess.WaitForExit();

            /* */
            txtDebug.AppendText("\r\n------------------------------------\r\n");
            txtDebug.AppendText("E\r\n");
            txtDebug.AppendText(exeOutputError);
            txtDebug.AppendText("\r\nS\r\n");
            txtDebug.AppendText(exeOutput);
            /* */

            //System.Threading.Thread.Sleep(500);

            if (returnMode == 1)
            {
                return exeOutput;
            }
            if (returnMode == 2)
            {
                return exeOutputError;
            }
            if (returnMode == 3)
            {
                return string.Concat(exeOutput, "\r\n", "%SPLITTER%", "\r\n", exeOutputError);
            }

            // invalid mode! (should never see this)
            return "INVALID MODE\r\n%SPLITTER%\r\nINVALID MODE";

        }

        private bool detectPapilioBoard(bool debugMode)
        {

            string PapilioBoardType = "";

            if (chkAutoDetect.Checked == false)
            {

                doLog(" - User Selected Papilio Device");

                if (cmbFPGA.Text == "P One")
                {
                    doLog(" - - Papilio One (500K) Selected");
                    PapilioBoardType = "XC3S500E";
                    lblPapilioDevice.Text = PapilioBoardType;
                    return true;
                }

                if (cmbFPGA.Text == "P Pro")
                {
                    doLog(" - - Papilio Pro Selected");
                    PapilioBoardType = "XC6SLX9-PRO";
                    lblPapilioDevice.Text = PapilioBoardType;
                    return true;
                }

                if (cmbFPGA.Text == "P DUO")
                {
                    doLog(" - - Papilio DUO Selected");
                    PapilioBoardType = "XC6SLX9-DUO";
                    lblPapilioDevice.Text = PapilioBoardType;
                    return true;
                }

            }
            else
            {

                doLog(" - Detecting Papilio Device");

				string CWD = string.Concat(Application.StartupPath, Path.DirectorySeparatorChar+ "papilio"+Path.DirectorySeparatorChar+ "tools" +Path.DirectorySeparatorChar );
                string tool = "papilio-prog.exe";

				if (Settings.IsUnix) {
					tool = "linux" + Path.DirectorySeparatorChar + "papilio-prog";
				}
                string toolArgs = "-j";

                try
                {
                    
                    string[] thisPapilio = RunEXE(CWD, tool, toolArgs, 1, debugMode).Split(':');

                    if (thisPapilio.Length == 3)
                    {

                        int papilioFound = 0;

                        switch (thisPapilio[2].Trim())
                        {
                            case "XC3S100E":
                                doLog(" - - Papilio One (100K) Detected");
                                PapilioBoardType = thisPapilio[2].Trim();
                                lblPapilioDevice.Text = PapilioBoardType;
                                papilioFound = 1;
                                break;

                            case "XC3S250E":
                                doLog(" - - Papilio One (250K) Detected");
                                PapilioBoardType = thisPapilio[2].Trim();
                                lblPapilioDevice.Text = PapilioBoardType;
                                papilioFound = 1;
                                break;

                            case "XC3S500E":
                                doLog(" - - Papilio One (500K) Detected");
                                PapilioBoardType = thisPapilio[2].Trim();
                                lblPapilioDevice.Text = PapilioBoardType;
                                papilioFound = 1;
                                break;

                            case "XC6SLX9":

                                toolArgs = "-j -d \"Papilio DUO A\"";
							    if (Settings.IsUnix)
							    {
								  toolArgs = "-j -d \"Papilio DUO\"";
							    }
                                thisPapilio = RunEXE(CWD, tool, toolArgs, 1, debugMode).Split(':');

                                if (thisPapilio.Length == 3)
                                {
                                    doLog(" - - Papilio DUO Detected");
                                    PapilioBoardType = "XC6SLX9-DUO";
                                    lblPapilioDevice.Text = PapilioBoardType;
                                    papilioFound = 1;
                                }
                                else
                                {

                                    toolArgs = "-j -d \"Dual RS232 A\"";
								    if (Settings.IsUnix)
								    {
									  toolArgs = "-j -d \"Dual RS232\"";
								    }
									
                                    thisPapilio = RunEXE(CWD, tool, toolArgs, 1, debugMode).Split(':');

                                    if (thisPapilio.Length == 3)
                                    {
                                        doLog(" - - Papilio Pro Detected");
                                        PapilioBoardType = "XC6SLX9-PRO";
                                        lblPapilioDevice.Text = PapilioBoardType;
                                        papilioFound = 1;
                                    }
                                    else
                                    {
                                        doLog(" - - Aborting - No Supported Papilio Device Detected");
                                        PapilioBoardType = "None";
                                        lblPapilioDevice.Text = PapilioBoardType;
                                        return false;
                                    }
                                }
                                break;

                            default:
                                break;
                        }

                        if (papilioFound == 0)
                        {
                            doLog(" - - Aborting - No Supported Papilio Device Detected");
                            PapilioBoardType = "None";
                            lblPapilioDevice.Text = PapilioBoardType;
                            return false;
                        }
                        else
                        {
                            return true;
                        }

                    } else {
                        doLog(" - - Aborting - No Supported Papilio Device Detected");
                        PapilioBoardType = "None";
                        lblPapilioDevice.Text = PapilioBoardType;
                        
                    }
					return false;
                   
                }

                catch
                {
                    doLog(string.Concat(" - - Aborting - An error has occurred while trying to autodetect your Papilio device"));
                    PapilioBoardType = "None";
                    lblPapilioDevice.Text = PapilioBoardType;
                    return false;
                }

            }

            doLog(" - - Aborting - No Supported Papilio Device Detected");
            return false;

        }


        private bool clearPapilioBuildDirectory(bool debugMode)
        {
            doLog(" - Clearing `_tmp` directory");
			string PapilioTMPPath = string.Concat(Application.StartupPath, Path.DirectorySeparatorChar + "papilio"+ Path.DirectorySeparatorChar +"_tmp");
			Console.WriteLine("clearPapilioBuildDirectory");
			Console.WriteLine(PapilioTMPPath);
            try
            {
                // delete temp directory and recreate it..
                if (Directory.Exists(PapilioTMPPath))
                {
                    Directory.Delete(PapilioTMPPath, true);
                    
                }
				Directory.CreateDirectory(PapilioTMPPath);
                return true;
            }
            catch
            {
                doLog(" - - Aborting - Could not clear `_tmp` directory");
                return false;
            }
        }

        private bool doUnzipSelectedFile(bool debugMode)
        {
			string CWDTMP = string.Concat(Application.StartupPath, Path.DirectorySeparatorChar + "papilio" + Path.DirectorySeparatorChar+ "_tmp"); // unzip doesnt like trailing slash
			string zipFilename = string.Concat(Application.StartupPath,Path.DirectorySeparatorChar,lblDITPath.Text,Path.DirectorySeparatorChar,lblSITName.Text,".zip");
            
            doLog(string.Concat(" - Unzip `", lblDITPath.Text, "\\", lblSITName.Text, ".zip`"));

			Console.WriteLine (zipFilename);
			Console.WriteLine (CWDTMP);

			try {
				ZipFile.ExtractToDirectory (zipFilename, CWDTMP);
			} catch (Exception ex) {

				doLog("Unzip fail");
				Console.WriteLine (ex.StackTrace);
				return false;
			}
			                  
            return true;

        }

        private bool doCopyBaseHardwareToTempDir(string baseHardware, bool debugMode)
        {

            doLog(" - Copying Base Hardware");
            
			string CWDTMP = string.Concat(Application.StartupPath, Path.DirectorySeparatorChar + "papilio"+ Path.DirectorySeparatorChar + "_tmp" +  Path.DirectorySeparatorChar);
			string CWDHW = string.Concat(Application.StartupPath, Path.DirectorySeparatorChar + "papilio"+ Path.DirectorySeparatorChar + "hardware" + Path.DirectorySeparatorChar, lblDITPath.Text.Replace("ROMRoot"+ Path.DirectorySeparatorChar,""), Path.DirectorySeparatorChar, lblPapilioDevice.Text, Path.DirectorySeparatorChar,baseHardware);
            
            string baseBITFile = string.Concat(CWDHW, ".bit");
            string baseBMMFile = string.Concat(CWDHW, "_bd.bmm");
            

			Console.WriteLine (CWDTMP);
			Console.WriteLine (CWDHW);


            if (System.IO.File.Exists(baseBITFile))
            {
                try
                {
                    File.Copy(baseBITFile, string.Concat(CWDTMP, "intermediate.bit"), true);
                    File.Copy(baseBMMFile, string.Concat(CWDTMP, "intermediate_bd.bmm"), true);
                    return true;
                }
                catch
                {
                    doLog(string.Concat(" - - Aborting - Could not copy base/bmm files to `papilio\\_tmp\\`"));
                    doLog(string.Concat(" - - - BITfile: ", baseBITFile));
                    doLog(string.Concat(" - - - BMMfile: ", baseBMMFile));
                    return false;
                }
            }
            else
            {
                doLog(string.Concat(" - - Aborting - This game is not supported by your Papilio Device yet!"));
                doLog(string.Concat(" - - - BITfile: ",baseBITFile));
                doLog(string.Concat(" - - - BMMfile: ", baseBMMFile));
                return false;
            }
        }

        private bool parsePScriptMERGE(string[] cmdArray, bool debugMode)
        {
            
            if (cmdArray.Length == 3)
            {
                // NOOP
            }
            else
            {
                doLog(" - - Aborting - Malformed `merge` PScript directive");
                return false;
            }

			string CWD = string.Concat(Application.StartupPath,  Path.DirectorySeparatorChar + "papilio" + Path.DirectorySeparatorChar +"_tmp" +  Path.DirectorySeparatorChar);
            string sourceFiles = cmdArray[1];
            string destinationFile = cmdArray[2];
			Console.WriteLine (CWD);
            //lstLogs.Items.Add(cmdArray[0]);
            doLog(" - - Processing `merge` PScript directive");
            try
            {
                using (FileStream outputFileStream = File.Create(string.Concat(CWD, destinationFile)))
                {
                    string[] baseFiles = sourceFiles.Split('|');

                    foreach (string baseFile in baseFiles)
                    {
						Console.WriteLine (baseFile);
						if (System.IO.File.Exists(string.Concat(CWD, baseFile)))
                        {
                            using (FileStream fs = File.OpenRead(string.Concat(CWD, baseFile)))
                            {
                                doLog(string.Concat(" - - - Merging `", baseFile, "` -> `", destinationFile,"`"));
                                CopyStream(outputFileStream, fs);
                            }
                        }
                        else
                        {
                            doLog(string.Concat(" - - Aborting - `", string.Concat(CWD, baseFile), "` not found"));
                            return false;
                        }
                    }
                    return true;
                }
            }

            catch
            {
                doLog(" - - Aborting - An error occured processing `merge` PScript directive");
                return false;
            }
        }

		private bool romGen(string binFile, string memFile, string str_addr_bits)
		{

			const int MAX_ROM_SIZE = 0x4000;
			string hexOutput;
			int i,j;

			int rom_inits;


			int number_of_block_rams =1;
			int block_ram_width = 8;
			int data;
			int mask,k;

			int addr_bits = int.Parse (str_addr_bits);

			int[] mem = new int[MAX_ROM_SIZE]; 

			Console.WriteLine ("RomGen bin: " + binFile);
			Console.WriteLine ("Mem File: " + memFile);
			Console.WriteLine ("Addr bits: " + str_addr_bits);


			// open bin file
			BinaryReader b = new System.IO.BinaryReader(File.Open(binFile, FileMode.Open));

			if ( b.BaseStream.Length > MAX_ROM_SIZE) {
				// bin file is to big
				doLog("--ROM size is greater then 4k --");
				return false;
			}	

			// read bin file
			for (int a = 0; a < b.BaseStream.Length; a++) {	
				mem[a]=  b.ReadByte ();
			}
			// create mem file
			System.IO.StreamWriter outFile = new System.IO.StreamWriter(memFile);


			rom_inits = 64;

			// ram16s
			switch (addr_bits) {
			case 14:
				number_of_block_rams = 8;
				block_ram_width = 1;
				break;
			case 13 : 
				number_of_block_rams = 4;
				block_ram_width = 2;
				break;
			case 12 : 
				number_of_block_rams = 2;
				block_ram_width = 4;
				break;
			default : break;
			}

			for (k = 0; k < number_of_block_rams; k++) {

				for (j = 0; j < rom_inits; j++) {

					switch (block_ram_width) {

					case 1: // width 1
						mask = 0x1 << (k);		  

						for (i = 248; i >= 0; i -= 8) {
							data = ((mem [(j * 256) + (255 - i)] & mask) >> k);
							data <<= 1;
							data += ((mem [(j * 256) + (254 - i)] & mask) >> k);
							data <<= 1;
							data += ((mem [(j * 256) + (253 - i)] & mask) >> k);
							data <<= 1;
							data += ((mem [(j * 256) + (252 - i)] & mask) >> k);
							data <<= 1;
							data += ((mem [(j * 256) + (251 - i)] & mask) >> k);
							data <<= 1;
							data += ((mem [(j * 256) + (250 - i)] & mask) >> k);
							data <<= 1;
							data += ((mem [(j * 256) + (249 - i)] & mask) >> k);
							data <<= 1;
							data += ((mem [(j * 256) + (248 - i)] & mask) >> k);

							hexOutput = String.Format("{0:X2} ", data);
							outFile.Write(hexOutput);
						}
						break;

					case 2: // width 2
								  
						mask = 0x3 << (k * 2);
						//for (i = 0; i < 128; i+=4) {
						for (i = 124; i >= 0; i -= 4) {
							data = ((mem [(j * 128) + (127 - i)] & mask) >> k * 2);
							data <<= 2;
							data += ((mem [(j * 128) + (126 - i)] & mask) >> k * 2);
							data <<= 2;
							data += ((mem [(j * 128) + (125 - i)] & mask) >> k * 2);
							data <<= 2;
							data += ((mem [(j * 128) + (124 - i)] & mask) >> k * 2);
							hexOutput = String.Format("{0:X2} ", data);
							outFile.Write(hexOutput);
						}
						break;

					case 4: // width 4
								  
						mask = 0xF << (k * 4);
						for (i = 0; i < 64; i += 2) {
							data = ((mem [(j * 64) + (63 - i)] & mask) >> k * 4);
							data <<= 4;
							data += ((mem [(j * 64) + (62 - i)] & mask) >> k * 4);

							hexOutput = String.Format("{0:X2} ", data);
							outFile.Write(hexOutput);
						}
						break;


					case 8: // width 8

						for (i = 31; i >= 0; i--) {
							data = ((mem [(j * 32) + (31 - i)]));
							hexOutput = String.Format("{0:X2} ", data);
							outFile.Write(hexOutput);
						}
						break;
					} 

				}

			}
			// close file
			outFile.Close ();
			return true;
		}
	

        private bool parsePScriptROMGEN(string[] cmdArray, bool debugMode)
        {
			string CWDTMP = string.Concat(Application.StartupPath,  Path.DirectorySeparatorChar + "papilio" + Path.DirectorySeparatorChar +"_tmp" +  Path.DirectorySeparatorChar);
			string CWDPATCH = string.Concat(Application.StartupPath,  Path.DirectorySeparatorChar + "papilio" + Path.DirectorySeparatorChar + "patches" + Path.DirectorySeparatorChar, lblDITPath.Text.Replace("ROMRoot" + Path.DirectorySeparatorChar,""), + Path.DirectorySeparatorChar);
			string CWD = string.Concat(Application.StartupPath,  Path.DirectorySeparatorChar + "papilio" +  Path.DirectorySeparatorChar +"tools" +  Path.DirectorySeparatorChar );

            doLog(" - - Processing `romgen` PScript directive");

			Console.WriteLine("parsePScriptROMGEN");
			Console.WriteLine(CWDTMP);
			Console.WriteLine(CWDPATCH);
			Console.WriteLine(CWD);
            try
            {
                if (cmdArray.Length == 4 || cmdArray.Length == 5)
                {
					
                    if (cmdArray.Length == 5)
                    {
						if(Settings.IsUnix)
						{	
							doLog((" - - - - Fail !!! No romgen patch support under linux.... `"));
							return false;	
						}
						// use -ini: switch to decrypt something! (TODO:: this needs love.. there is no patch sorting at all atm.. put in dir named after dat or something)
                        // lstLogs.Items.Add("saving decrypted rg output to " + cmdArray[2] + ".mem");
                        doLog(string.Concat(" - - - ROMGen: `", cmdArray[1], "` -> `", cmdArray[2], ".mem`", " Patch: `", cmdArray[4],"`"));
						Console.WriteLine(CWDTMP);
						using (StreamWriter outfile = new StreamWriter(string.Concat(CWDTMP, cmdArray[2], ".mem")))
                        {
                            if (System.IO.File.Exists(string.Concat(CWDPATCH, cmdArray[4])))
                            {
                                if (System.IO.File.Exists(string.Concat(CWDTMP, cmdArray[1])))
                                {
									string ROMGenOutput;
									if (Settings.IsUnix){
										Console.WriteLine("romgen args");
										Console.WriteLine(CWD);
										string myargs= string.Concat("\"", CWDTMP, cmdArray[1], "\" ", cmdArray[2], " ", cmdArray[3], " m r e -ini:\"", CWDPATCH, cmdArray[4], "\"");
										Console.WriteLine(myargs);
										ROMGenOutput = RunEXE(CWD, "linux"+ Path.DirectorySeparatorChar+"romgen",myargs , 1, debugMode);
									}

									else {
									    ROMGenOutput = RunEXE(CWD, "romgen.exe", string.Concat("\"", CWDTMP, cmdArray[1], "\" ", cmdArray[2], " ", cmdArray[3], " m r e -ini:\"", CWDPATCH, cmdArray[4], "\""), 1, debugMode);
									}
									outfile.Write(ROMGenOutput);
                                    return true;
                                }
                                else
                                {
                                    doLog(string.Concat(" - - - - Aborting - `", CWDTMP, cmdArray[1], "` not found"));
                                    return false;
                                }
                            }
                            else
                            {
                                doLog(string.Concat(" - - - - Aborting - Patch Not Found: `", cmdArray[4],"`"));
                                return false;
                            }
                        }
                    }
                    else
                    {
                        // no -ini: switch
					    return romGen(CWDTMP + cmdArray[1], CWDTMP + cmdArray[2] + ".mem", cmdArray[3]);
                     
                    }
                }
                else
                {
                    doLog(" - - Aborting - An error occured processing `romgen` PScript directive (args)");
                    progressLoadFPGA.Refresh();
                    return false;
                }
            }

            catch
            {
                doLog(" - - Aborting - An error occured processing `romgen` PScript directive");
                
            }
			return false;

        }

        private bool parsePScriptDATA2MEM(string[] cmdArray, bool debugMode)
        {
			string CWDTMP = string.Concat(Application.StartupPath, Path.DirectorySeparatorChar + "papilio" + Path.DirectorySeparatorChar +"_tmp" + Path.DirectorySeparatorChar);
			string CWD = string.Concat(Application.StartupPath, Path.DirectorySeparatorChar + "papilio" + Path.DirectorySeparatorChar +"tools" + Path.DirectorySeparatorChar);

			string tool;

			if (Settings.IsUnix) {
				tool = "linux" + Path.DirectorySeparatorChar + "data2mem";
			} else {
			    tool = "data2mem.exe";
			}
            string toolArgs = string.Concat("-bm \"" ,CWDTMP,"intermediate_bd.bmm\" -bt \"" , CWDTMP,"intermediate.bit\" -bd \"" , CWDTMP,cmdArray[1] , ".mem\" tag avrmap." , cmdArray[2] , " -o b \"" , CWDTMP,"processed.bit\"");

			Console.WriteLine ("data2mem args");
			Console.WriteLine (toolArgs);
            doLog(" - - Processing `data2mem` PScript directive");

			if (cmdArray.Length == 3)
			{
			    // run data2mem
			    // rename processed.bit to intermediate.bit so we can continue processing later..
                if (File.Exists(string.Concat(CWDTMP, "processed.bit")))
                {
                    try
                    {
                        if (File.Exists(string.Concat(CWDTMP, "intermediate.bit")))
                        {
                            File.Delete(string.Concat(CWDTMP, "intermediate.bit"));
                        }
                        File.Move(string.Concat(CWDTMP, "processed.bit"), string.Concat(CWDTMP, "intermediate.bit"));
                    }
                    catch
                    {
                        doLog(" - - - Aborting - Could Not Move Intermediate File!");
                        progressLoadFPGA.Refresh();
                        return false;
                    }
                }

                //doLog(" - - Processing `data2mem` PScript directive");
                doLog(string.Concat(" - - - DATA2MEM: `", cmdArray[1], ".mem`", " -> `avrmap.", cmdArray[2],"`"));
			    //doLog(string.Concat(" - - - Run PScript - DATA2MEM: ", cmdArray[1], ".mem", " -> avrmap.", cmdArray[2]));
			    string data2mem_output = RunEXE(CWD, tool, toolArgs, 1, debugMode);
                if (data2mem_output.Trim().StartsWith("ERROR"))
                {
                    doLog(" - - - - Aborting - An error occured processing `data2mem` PScript directive");
                    return false;
                }
                else
                {
                    return true;
                }
			}
			else
			{
                doLog(" - - - Aborting - An error occured processing `data2mem` PScript directive (args)");
                progressLoadFPGA.Refresh();
                return false;
			}
            
        }

        private bool saveGeneratedBitfile(bool debugMode)
        {
            // Save Generated Bitfile to Library

            doLog(" - - - Saving Generated Bitfile to Library");

            //dlgLoadBitfile.InitialDirectory
			dlgSaveBitfile.InitialDirectory = string.Concat(Application.StartupPath,  Path.DirectorySeparatorChar + "papilio" + Path.DirectorySeparatorChar + "library" +  Path.DirectorySeparatorChar);
            dlgSaveBitfile.Title = "Save Generated Bitfile to Library";
			string CWDTMP = string.Concat(Application.StartupPath,  Path.DirectorySeparatorChar + "papilio" + Path.DirectorySeparatorChar + "_tmp" +  Path.DirectorySeparatorChar);

            try
            {
                dlgSaveBitfile.FileName = string.Concat(lblDITName.Text, " - ", lblSITDescription.Text, ".bit");
            }
            catch
            {
                dlgSaveBitfile.FileName = "Generated.bit";
            }

            if (dlgSaveBitfile.ShowDialog() == DialogResult.OK)
            {
                //MessageBox.Show(dlgSaveBitfile.FileName);
                File.Copy(string.Concat(CWDTMP, "processed.bit"), dlgSaveBitfile.FileName, true);
                clearPapilioBuildDirectory(false);
                return true;

                //File.Copy(string.Concat(dlgLoadBitfile.FileName), string.Concat(CWDTMP, dlgLoadBitfile.SafeFileName), true);
                //clearPapilioBuildDirectory(false);
            }
          
            return false;
            

        }

        private bool parsePScriptPAPILIOPROG(string[] cmdArray, bool debugMode)
        {
            
            doLog(" - - Loading Bitfile");
            
			string CWDTMP = string.Concat(Application.StartupPath,  Path.DirectorySeparatorChar + "papilio" +  Path.DirectorySeparatorChar + "_tmp" +  Path.DirectorySeparatorChar);
			string CWD = string.Concat(Application.StartupPath,  Path.DirectorySeparatorChar + "papilio" +  Path.DirectorySeparatorChar + "tools" +  Path.DirectorySeparatorChar);

            string tool = "papilio-prog.exe";
			if (Settings.IsUnix) {
				tool = "linux"+ Path.DirectorySeparatorChar+ "papilio-prog";
			}
            string toolArgs = "-v -f";

            string bitfileFilename = "";
            if (cmdArray.Length == 2)
            {
                // we specified a filename so lets use that
                bitfileFilename = string.Concat(CWDTMP, cmdArray[1]);
            }
            else
            {
                // no bitfile specified so load generated (processed.bit) bitfile
                bitfileFilename = string.Concat(CWDTMP, "processed.bit");
            }

            if (debugMode == true)
            {
                lstLogs.Items.Add(string.Concat(" - - - debug -- bitfile to load - ", bitfileFilename));
            }

            if (System.IO.File.Exists(bitfileFilename))
            {
                if (cmbProgramTarget.Text == "FPGA")
                {
                    doLog(" - - - to FPGA");

					string prog_args = string.Concat (toolArgs, " ", "\"", bitfileFilename, "\"");
					Console.WriteLine("papilio-prog args");
					Console.WriteLine(prog_args);

					string papilioprog_output = RunEXE(CWD, tool, prog_args , 1, debugMode);
					lstLogs.Items.Add(papilioprog_output);
                    if (chkSaveGeneratedBitfile.Checked == false)
                    {
                        clearPapilioBuildDirectory(false);
                    }
                    else
                    {
                        saveGeneratedBitfile(debugMode);
                    }

                    return true;
                }
                if (cmbProgramTarget.Text == "Flash")
                {
                    doLog(" - - - to Flash");
                    // TO test
					string bscanFile = string.Concat(Application.StartupPath,  Path.DirectorySeparatorChar + "papilio" +  Path.DirectorySeparatorChar + "bscanSPI" +  Path.DirectorySeparatorChar, lblPapilioDevice.Text,".bit");
                    if (System.IO.File.Exists(bscanFile))
                    {

                        // " -v -f", bitfileFilename, " -b \"", bscanFile, "\" -sa  -r";

						string prog_args = string.Concat (" -v -f", bitfileFilename, " -b \"", bscanFile, "\" -sa  -r");
						Console.WriteLine("papilio-prog args");
						Console.WriteLine(prog_args);

						string papilioprog_output = RunEXE(CWD, tool, prog_args, 1, debugMode);
						lstLogs.Items.Add(papilioprog_output);

						doLog(" - - - - Triggering FPGA Reconfiguration");
                        papilioprog_output = RunEXE(CWD, tool, " -c", 1, debugMode);

                        if (chkSaveGeneratedBitfile.Checked == false)
                        {
                            clearPapilioBuildDirectory(false);
                        }
                        else
                        {
                            saveGeneratedBitfile(debugMode);
                        }

                        return true;
                    }
                    else
                    {
                        doLog(string.Concat(" - - - - Cannot find: ",bscanFile," Loading to FPGA instead"));
                        string papilioprog_output = RunEXE(CWD, tool, string.Concat(toolArgs, " ", bitfileFilename), 1, debugMode);
						lstLogs.Items.Add(papilioprog_output);
						return false;
                    }
                    
                }
                doLog(" - - - - Aborting - Invalid Load Option");
                return true;
            }
            else
            {
                lstLogs.Items.Add(" - - - Aborting - Cannot Load Bitfile - File Not Found");
                return false;
            }

            //return false;
        }

        private void papilioParsePapilioScript(RvDir tGame)
        {
            
            lstLogs.Items.Clear();
            txtDebug.Clear();

            bool debugMode = false;

            if (debugMode == true)
            {
                lstLogs.Items.Add("");
                lstLogs.Items.Add("This build was compiled with debugging enabled!");
                lstLogs.Items.Add("--------------------------------------------------");
                lstLogs.Items.Add("");
            }

            // detect papilio fpga type
            if (detectPapilioBoard(debugMode) == false)
            {
				
				return;
            }

            doLog(""); // spacer

            // clear temp directory
            if (clearPapilioBuildDirectory(debugMode) == false)
            {
                return;
            }

            doLog("");

            // unzip selected item from the GameGrid
            if (doUnzipSelectedFile(debugMode) == false)
            {
                return;
            }

            doLog(""); // spacer
            
            string pscriptA = "";

            // copy base hardware bitfile
            string baseHardware = tGame.Game.GetData(RvGame.GameData.papilioHardware);
            if (baseHardware.StartsWith("autodetect:"))
            {
                string autodetectHardware = baseHardware.Substring(11);
                switch (autodetectHardware.ToUpper())
                {
				case "A2601":
					doLog (" - AutoDetecting Cartridge Type for A2601 Hardware");
					string bssType = autodetectA2601 (tGame);

					doLog (" - - Detected - " + bssType);
					Console.WriteLine(" - - Detected - " + bssType);

                        switch (bssType)
                        {
                            case "2K":
                                // override bss
                                baseHardware = "A2601-2K4K";
                                // override pscript (same as normal A2601 but doubles 2K cart_rom to 4K)
                                pscriptA = "merge;%romname%|%romname%;cart_rom.bin\nromgen;cart_rom.bin;cart_rom;14\ndata2mem;cart_rom;rom_code\npapilio-prog";
                                break;

                            case "4K":
                                // override bss
                                baseHardware = "A2601-2K4K";
                                // A2601 pscript
                                pscriptA = "romgen;%romname%;cart_rom;14\ndata2mem;cart_rom;rom_code\npapilio-prog";
                                break;

                            default:
                                // A2601 pscript
                                baseHardware = string.Concat("A2601-", bssType);
                                pscriptA = "romgen;%romname%;cart_rom;14\ndata2mem;cart_rom;rom_code\npapilio-prog";
                                break;
                        }
                        //baseHardware = string.Concat("A2601-", bssType);
                        pscriptA = pscriptA.Replace("%romname%",RomGrid.Rows[0].Cells["CRom"].Value.ToString().Trim());
                        break;

				default:
					doLog (string.Concat ("--Aborting-- Unrecognized AutoDetect Parameter `", autodetectHardware.ToUpper (), "`"));
					return;
                    break;
					    
                }

               

            }
            else
            {
                
                pscriptA = tGame.Game.GetData(RvGame.GameData.papilioScript);

            }

            if (doCopyBaseHardwareToTempDir(baseHardware, debugMode) == false)
            {
                doLog("--Cannot copy base hardware--");
                return;
            }


            doLog(""); // spacer

      
            string[] pscript = pscriptA.Trim().Split('\n');

            int i = 0;
            progressLoadFPGA.Maximum = pscript.Length;
            progressLoadFPGA.Minimum = 0;
            progressLoadFPGA.Value = 0;
            foreach (string pscriptDirective in pscript)
            {
                string[] splitDirective = pscriptDirective.Trim().Split(';');
                string thisDirective = splitDirective[0].Trim().ToLower();
                if (debugMode == true)
                {
                    lstLogs.Items.Add(string.Concat("deBug: Directive=`", thisDirective, "`"));
                }
                if (splitDirective.Length == 0)
                {
                }
                else
                {
                    switch (thisDirective)
                    {
                        case "":
                            break;

                        case "merge":
                            doLog(string.Concat(" - Parse PScript Directive `",thisDirective,"`"));
                            if (parsePScriptMERGE(splitDirective, debugMode) == false)
                            {
                                return;
                            }
                            break;

                        case "romgen":
                            doLog(string.Concat(" - Parse PScript Directive `", thisDirective, "`"));
                            if (parsePScriptROMGEN(splitDirective, debugMode) == false)
                            {
                                return;
                            }
                            break;

                        case "data2mem":
                            doLog(string.Concat(" - Parse PScript Directive `", thisDirective, "`"));
                            if (parsePScriptDATA2MEM(splitDirective, debugMode) == false)
                            {
                                return;
                            }
                            break;

                        case "papilio-prog":
                            doLog(string.Concat(" - Parse PScript Directive `", thisDirective, "`"));
                            if (parsePScriptPAPILIOPROG(splitDirective, debugMode) == false)
                            {
                                return;
                            }
                            break;

                        default:
                            doLog(string.Concat(" - Parse PScript Directive `", thisDirective, "`"));
                            doLog(string.Concat(" - - Invalid Directive `", thisDirective, "`. Aborting."));
                            break;
                    }
                }

                i++;
                progressLoadFPGA.Value = i;
                progressLoadFPGA.Refresh();
                doLog(""); // spacer
                
            }

        }
    }
}

