﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Office.Interop.Excel;
using XlApplication = Microsoft.Office.Interop.Excel.Application;

namespace ACadLib.Utilities
{
    /// <summary>
    /// Represents a Design sheet in Excel
    /// </summary>
    public class DesignSheet : IDisposable
    {
        /// <summary>
        /// The Excel Application
        /// </summary>
        public readonly XlApplication XlApp;

        /// <summary>
        /// The Workbook
        /// </summary>
        private readonly Workbook _xlWorkbook;

        /// <summary>
        /// The PipeData Worksheet
        /// </summary>
        public readonly Worksheet PipeDataSheet;

        /// <summary>
        /// Dictionary of Named Ranges
        /// </summary>
        public Dictionary<string, Range> XlWorkbookNames { get; }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="filename">Workbook filename</param>
        /// <param name="pipeDataSheetName">The pipe data in sheet name</param>
        public DesignSheet(string filename, string pipeDataSheetName)
        {
            if ( filename == null )
            {
                MessageBox.Show("You must provide a filename", "AUTOSHEET ERROR", MessageBoxButton.OK);
                return;
            }

            if ( !File.Exists(filename) || Path.GetExtension(filename) != ".xlsm" )
            {
                MessageBox.Show($"Error Opening File: {filename} doesn't exist or is the wrong file extension", "AUTOSHEET ERROR", MessageBoxButton.OK);
                return;
            }

            XlApp = new XlApplication()
            {
                Visible = true
            };

            _xlWorkbook = null;
            PipeDataSheet = null;

            _xlWorkbook = XlApp.Workbooks.Open(filename);

            // Get the worksheets
            PipeDataSheet = _xlWorkbook.Worksheets[pipeDataSheetName];

            if (_xlWorkbook == null || PipeDataSheet == null)
                throw new COMException();

            // Get and Set the Workbook names
            XlWorkbookNames = new Dictionary<string, Range>();
            foreach ( Name name in _xlWorkbook.Names )
            {
                try
                {
                    XlWorkbookNames.Add(name.Name, name.RefersToRange);
                }
                catch ( COMException e )
                {
                    Debug.WriteLine($"DesignSheet Class threw {e.GetType()}: {e} when loading name in Dictionary");
                }
            }
        }

        /// <summary>
        /// Finalizer for this class
        /// </summary>
        ~DesignSheet()
        {
            ReleaseUnmanagedResources();
        }

        /// <summary>
        /// Returns true if the design sheet is ready
        /// and working properly
        /// </summary>
        /// <returns>Returns true if the design sheet is working properly</returns>
        public bool IsReady()
        {
            try
            {
                // Try to call the worksheets object
                var test = XlApp.Worksheets;
            }
            catch (System.Exception)
            {
                // If anything goes wrong then the sheet is
                // not working properly
                return false;
            }

            return true;
        }

        /// <summary>
        /// Dispose of this object releasing all of its
        /// unmanaged resources and COM objects
        /// </summary>
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

        /// <summary>
        /// Get the Excel Process of this object
        /// </summary>
        private Process ExcelProcess
        {
            get
            {
                GetWindowThreadProcessId(XlApp.Hwnd, out var id);
                return Process.GetProcessById(id);
            }
        }

        /// <summary>
        /// Release all of the unmanaged resources and COM objects
        /// </summary>
        private void ReleaseUnmanagedResources()
        {
            if ( PipeDataSheet != null )
                Marshal.FinalReleaseComObject(PipeDataSheet);

            if (_xlWorkbook != null)
                Marshal.FinalReleaseComObject(_xlWorkbook);

            if ( XlApp == null ) return;

            ExcelProcess?.Kill();
            Marshal.FinalReleaseComObject(XlApp);
        }


    }
}
