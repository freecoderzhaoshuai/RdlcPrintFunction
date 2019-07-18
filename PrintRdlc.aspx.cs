using Dispatch.Helper;
using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Dispatch
{
    public partial class TestRdlc : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }


        public void GenerateGridBody(out DataTable gridTable,DataTable dtbody,string productDiameter,string symbol)
        {
            //plus sph count
            var  count = dtbody.AsEnumerable().
                Where(row => row.Field<String>("comdia") == productDiameter && row.Field<String>("sph").StartsWith(symbol)).ToList().Count;

            if (count == 0)
            {
                gridTable = dtbody.AsEnumerable().GroupBy(x => x["cyl"]).Select(s => s.First()).CopyToDataTable();
                DataRow[] rows = gridTable.Select();
                for (int i = 0; i < rows.Length; i++)
                {
                    rows[i]["sph"] = null;
                }
            }
            else
            {
                gridTable = dtbody.AsEnumerable()
    .Where(row => row.Field<String>("comdia") == productDiameter && row.Field<String>("sph").StartsWith(symbol))
    .CopyToDataTable();
            }

        }



        public void PrintGrid(string gridPath, Zen.Barcode.Code128BarcodeDraw barcode, string invoiceNo, DataTable dtheadNew, DataRow drhead,DataTable gridTableRight,DataTable gridTableLeft)
        {
            LocalReport localReport = new LocalReport(); 
            localReport.ReportPath = Server.MapPath(gridPath);

            var image = barcode.Draw(invoiceNo, 50, 2);
            byte[] arr;
            using (var memStream = new MemoryStream())
            {
                image.Save(memStream, ImageFormat.Jpeg);
                arr = memStream.ToArray();
            }
            drhead["Barcode"] = arr;

            dtheadNew.ImportRow(drhead);

            localReport.DataSources.Add(new ReportDataSource("GridHead", dtheadNew));
            if (gridTableLeft != null && gridTableRight != null)
            {
                localReport.DataSources.Add(new ReportDataSource("Grid200BodyRight", gridTableRight));
            }
            localReport.DataSources.Add(new ReportDataSource("Grid200BodyLeft", gridTableLeft));
            string printerName = "Microsoft Print to PDF";
            localReport.PrintToPrinter(printerName);
            dtheadNew.Rows.Clear();

        }




        protected void Button1_Click(object sender, EventArgs e)
        {
            DataSet dataset = new DataSet();

            using (SqlConnection connection = new SqlConnection(Connection.conn))
            {
                using (SqlDataAdapter da = new SqlDataAdapter("GetPrintInvoiceLayoutInfo", connection))
                {
                    da.SelectCommand.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand.Parameters.Add(new SqlParameter() { ParameterName = "@trackno", SqlDbType = SqlDbType.VarChar, Size = 15, Value = "105017245550" });
                    da.SelectCommand.Parameters.Add(new SqlParameter() { ParameterName = "@entity", SqlDbType = SqlDbType.NVarChar, Size = 20, Value = "PR_CHLOE" });
                    da.Fill(dataset);
                    da.SelectCommand.Parameters.Clear();
                }
            }
            DataTable dthead = dataset.Tables[4];
            dthead.Columns.Add("Barcode", typeof(Byte[]));
            DataTable dtheadNew = new DataTable();
            dtheadNew = dthead.Clone();

            DataTable dtbody = dataset.Tables[5];

            string grid400Path = "~/GridReport/Grid400.rdlc";

            string grid200Path = "~/GridReport/Grid200.rdlc";
            Zen.Barcode.Code128BarcodeDraw barcode = Zen.Barcode.BarcodeDrawFactory.Code128WithChecksum;

            foreach (DataRow drhead in dthead.Rows)
            {
               
                string invoiceNo = drhead["InvoiceNo"].ToString();

                string productDiameter = drhead["ProductDiameter"].ToString();

                var cylPlusNum = dtbody.AsEnumerable().Where(row => row.Field<String>("comdia") == productDiameter && row.Field<String>("sph").StartsWith("+")).GroupBy(x => x["cyl"]).Select(s => s.First()).Count();

                var cylMinusNum = dtbody.AsEnumerable().Where(row => row.Field<String>("comdia") == productDiameter && row.Field<String>("sph").StartsWith("-")).GroupBy(x => x["cyl"]).Select(s => s.First()).Count();

                if (cylPlusNum <= 9 && cylMinusNum <= 9)
                {
                  
                    DataTable grid200Plus = new DataTable();

                    GenerateGridBody(out grid200Plus, dtbody, productDiameter,"+");


                    DataTable grid200Minus = new DataTable();

                    GenerateGridBody(out grid200Minus, dtbody, productDiameter,"-");

                    PrintGrid(grid200Path, barcode, invoiceNo, dtheadNew, drhead, grid200Minus, grid200Plus);


                }
                else
                {
                    string[] symbols ={"+","-"};
                    foreach (string s in symbols)
                    {
                        DataTable grid400Left = new DataTable();

                        GenerateGridBody(out grid400Left, dtbody, productDiameter, s);
                        PrintGrid(grid400Path, barcode, invoiceNo, dtheadNew, drhead, null, grid400Left);
                    }
                   
                }

            }
        }
    }
}
