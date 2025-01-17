﻿using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;

namespace Masuit.Tools.Excel
{
    public static class ExcelExtension
    {
        static ExcelExtension()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// 将内存表自动填充到Excel
        /// </summary>
        /// <param name="sheetTables">sheet名和内存表的映射</param>
        /// <param name="password">密码</param>
        /// <returns>内存流</returns>
        public static MemoryStream DataTableToExcel(this Dictionary<string, DataTable> sheetTables, string password = null)
        {
            using (var pkg = new ExcelPackage())
            {
                foreach (var pair in sheetTables)
                {
                    pair.Value.TableName = pair.Key;
                    CreateWorksheet(pkg, pair.Value);
                }

                return SaveAsStream(pkg, password);
            }
        }

        /// <summary>
        /// 将内存表自动填充到Excel
        /// </summary>
        /// <param name="tables">内存表</param>
        /// <param name="password">密码</param>
        /// <returns>内存流</returns>
        public static MemoryStream DataTableToExcel(this List<DataTable> tables, string password = null)
        {
            using var pkg = new ExcelPackage();
            foreach (var table in tables)
            {
                CreateWorksheet(pkg, table);
            }

            return SaveAsStream(pkg, password);
        }

        /// <summary>
        /// 将内存表自动填充到Excel
        /// </summary>
        /// <param name="table">内存表</param>
        /// <param name="password">密码</param>
        /// <returns>内存流</returns>
        public static MemoryStream ToExcel(this DataTable table, string password = null)
        {
            using var pkg = new ExcelPackage();
            CreateWorksheet(pkg, table);
            return SaveAsStream(pkg, password);
        }

        private static MemoryStream SaveAsStream(ExcelPackage pkg, string password)
        {
            var ms = new MemoryStream();
            if (!string.IsNullOrEmpty(password))
            {
                pkg.SaveAs(ms, password);
            }
            else
            {
                pkg.SaveAs(ms);
            }

            return ms;
        }

        public static void CreateWorksheet(this ExcelPackage pkg, DataTable table)
        {
            if (string.IsNullOrEmpty(table.TableName))
            {
                table.TableName = "Sheet1";
            }

            pkg.Workbook.Worksheets.Add(table.TableName);
            var sheet = pkg.Workbook.Worksheets[table.TableName];

            // 填充表头
            var maxWidth = new int[table.Columns.Count];
            for (var j = 0; j < table.Columns.Count; j++)
            {
                sheet.SetValue(1, j + 1, table.Columns[j].ColumnName);
                maxWidth[j] = Encoding.UTF8.GetBytes(table.Columns[j].ColumnName).Length;
            }

            sheet.Row(1).Style.Font.Bold = true; // 表头设置为粗体
            sheet.Row(1).Style.Font.Size = sheet.Row(1).Style.Font.Size * 1.11f; // 表头字号放大1.11倍
            sheet.Row(1).CustomHeight = true; // 自动调整行高
            sheet.Cells.AutoFitColumns(); // 表头自适应列宽
            sheet.Cells.Style.WrapText = true;

            // 填充内容
            for (var i = 0; i < table.Rows.Count; i++)
            {
                sheet.Row(i + 2).CustomHeight = true; // 自动调整行高
                for (var j = 0; j < table.Columns.Count; j++)
                {
                    if (table.Rows[i][j] is Stream s)
                    {
                        if (s.Length > 0)
                        {
                            using var img = Image.FromStream(s);
                            using var bmp = new Bitmap(img);
                            bmp.SetResolution(96, 96);
                            using var picture = sheet.Drawings.AddPicture(Guid.NewGuid().ToString(), bmp);
                            picture.SetPosition(i + 1, 3, j, 5); //设置图片显示位置
                            sheet.Row(i + 2).Height = 90;
                            sheet.Column(j + 1).Width = 30;
                        }
                    }
                    else
                    {
                        sheet.SetValue(i + 2, j + 1, table.Rows[i][j] ?? "");

                        // 根据单元格内容长度来自适应调整列宽
                        maxWidth[j] = Math.Max(Encoding.UTF8.GetBytes(table.Rows[i][j].ToString() ?? string.Empty).Length, maxWidth[j]);
                        if (sheet.Column(j + 1).Width < maxWidth[j])
                        {
                            sheet.Cells[i + 2, j + 1].AutoFitColumns(18, 110); // 自适应最大列宽，最小18，最大110
                        }
                    }
                }
            }

            //打印方向：纵向
            sheet.PrinterSettings.Orientation = eOrientation.Landscape;

            //集中在一页里打印
            sheet.PrinterSettings.FitToPage = true;

            //使用A4纸
            sheet.PrinterSettings.PaperSize = ePaperSize.A4;
        }
    }
}
