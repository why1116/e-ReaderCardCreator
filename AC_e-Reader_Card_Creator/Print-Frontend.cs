﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using AC_e_Reader_Card_Creator.References;

namespace AC_e_Reader_Card_Creator
{
    public partial class Print_Frontend : Form
    {
        private bool isDarkModeEnabled;
        private PreferencesManager preferencesManager = new PreferencesManager("e-ReaderCardCreator");

        public Print_Frontend()
        {
            InitializeComponent();
            PopulatePrintersComboBox();
            LoadDarkModePreference();
            ApplyTheme();

            comboBox_DPI.SelectedIndex = 1; // 600 DPI default
        }

        private void ToggleDarkMode(object sender, EventArgs e)
        {
            isDarkModeEnabled = !isDarkModeEnabled;
            preferencesManager.SaveDarkModePreference(isDarkModeEnabled);
        }

        private void LoadDarkModePreference()
        {
            isDarkModeEnabled = preferencesManager.ReadDarkModePreference();
        }

        private void ApplyTheme()
        {
            BackColor = isDarkModeEnabled ? preferencesManager.DarkModeBackColor : SystemColors.Control;
            preferencesManager.ApplyThemeToControls(Controls, isDarkModeEnabled);
        }

        private void PopulatePrintersComboBox()
        {
            foreach (string printerName in PrinterSettings.InstalledPrinters)
            {
                comboBox_Printer.Items.Add(printerName);
            }

            if (comboBox_Printer.Items.Count > 0)
            {
                comboBox_Printer.SelectedIndex = 0;
            }

            GetSelectedPrinterDPI(comboBox_Printer.SelectedItem.ToString());
        }

        private void SelectPrinter(object sender, EventArgs e)
        {
            GetSelectedPrinterDPI(comboBox_Printer.SelectedItem.ToString());
        }

        private void GetSelectedPrinterDPI(string printerName)
        {
            PrinterSettings ps = new()
            {
                PrinterName = printerName
            };
            PrinterResolution pr = ps.DefaultPageSettings.PrinterResolution;

            if (ps.IsValid)
            {
                if (pr.Kind == PrinterResolutionKind.Custom)
                {
                    label_PrinterDPI.Text = $"{pr.X} x {pr.Y} DPI";
                }
                else
                {
                    label_PrinterDPI.Text = $"Preset: {pr.Kind}";
                }
            }
        }

        private void SaveBMP(object sender, EventArgs e)
        {
            if (comboBox_DPI.SelectedItem == null)
            {
                MessageBox.Show("Please select a dot code DPI", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int DPI = int.Parse(comboBox_DPI.Text[..^4]);

            SaveFileDialog saveRAWFile = new()
            {
                Filter = "Bitmap Images (*.bmp)|*.bmp",
                Title = "Save Dot Code (.bmp)",
                FileName = "dotcode"
            };

            if (saveRAWFile.ShowDialog() == DialogResult.OK)
            {
                ProcessStartInfo raw_to_bmp = new()
                {
                    FileName = Common.RAW2BMP,
                    Arguments = Common.RAW2BMP_ARGS(saveRAWFile.FileName.Replace(".bmp", ""), DPI),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using Process process = Process.Start(raw_to_bmp);
                process.WaitForExit();
            }
        }

        private void Print(object sender, EventArgs e)
        {
            if (comboBox_Printer.SelectedItem == null)
            {
                MessageBox.Show("No valid printers found!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int DPI = int.Parse(comboBox_DPI.Text[..^4]);
            MessageBox.Show($"{DPI}");

            ProcessStartInfo raw_to_bmp = new()
            {
                FileName = Common.RAW2BMP,
                Arguments = Common.RAW2BMP_ARGS(Common.BMP_DOTCODE, DPI),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using (Process process = Process.Start(raw_to_bmp)) { process.WaitForExit(); }

            PrintDotCode();
        }

        private void PrintDotCode()
        {
            string selectedPrinter = comboBox_Printer.SelectedItem.ToString();
            PrintDocument printDocument = new();
            printDocument.PrinterSettings.PrinterName = selectedPrinter;

            printDocument.PrintPage += (sender, e) =>
            {
                Image image = Image.FromFile(Common.BMP_DOTCODE + ".bmp");
                Point loc = new(0, 0);
                e.Graphics.DrawImage(image, loc);
            };

            try
            {
                printDocument.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while printing: {ex.Message}");
            }
        }
    }
}
