using MigraDoc.DocumentObjectModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Models
{
    public class YVFonts
    {
        public static Color borderColor = Color.Parse("#007dae");
        public static Color tableEdgeColor = Color.Parse("#BDBEC1");
      
        public static Color shadingColor = Color.Parse("#E2EDF6");
        public static Font fontSmall;
        public static Font font9B;
        public static Font font9_5;
        public static Font font10;
        public static Font font11;
        public static Font font11Underline;

        public static Font font8_5;
        public static Font fontBig;
        public static Font font12B;
        public static Font fontMedium;
        public static Font fontTotal;
        public static Font fontTotalWithDiscount;
        public static Font fontTiny;
        public static Font fontTableColumnsHeader;
        public static Font fontLine;
        public static Font fontBlueLine;
        public static Font fontFooter;
        public static Font fontWhite;

        public static Font fontRed;

        static YVFonts()
        {
            fontSmall = new Font();
            fontSmall.Size = 9;

            font9B = new Font();
            font9B.Size = 9;
            font9B.Bold = true;

            font9_5 = new Font();
            font9_5.Size = 9.5;



            font10 = new Font();
            font10.Size = 10;

            font11 = new Font();
            font11.Size = 11;

            font11Underline = new Font();
            font11Underline.Size = 11;
            font11Underline.Underline = Underline.Single;

            fontBig = new Font();
            fontBig.Size = 11;
            fontBig.Bold = true;


            font12B = new Font();
            font12B.Size = 12;
            font12B.Bold = true;

            fontMedium = new Font();
            fontMedium.Size = 10;
            fontMedium.Bold = true;
            fontTotal = new Font();
            fontTotal.Size = 15;
            fontTotal.Bold = true;

            fontTotalWithDiscount = new Font();
            fontTotalWithDiscount.Size = 13;

            fontTiny = new Font();
            fontTiny.Size = 8;

            font8_5 = new Font();
            font8_5.Size = 8.5;


            fontTableColumnsHeader = new Font();
            fontTableColumnsHeader.Size = 9;
            fontTableColumnsHeader.Bold = true;



            fontLine = new Font();
            fontLine.Color = Color.Parse("#BDBEC1");

            fontBlueLine = new Font();
            fontBlueLine.Color = borderColor;

            fontFooter = new Font();
            fontFooter.Size = 9;
            fontFooter.Bold = true;

            fontWhite = new Font();
            fontWhite.Color= Color.Parse("#FFFFFF");

            fontRed = new Font();
            fontRed.Color = Color.Parse("#ff0000");

        }


    }
}
