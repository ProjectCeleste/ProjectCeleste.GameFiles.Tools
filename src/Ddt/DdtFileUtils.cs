﻿using System.Drawing.Imaging;
using System.IO;

namespace ProjectCeleste.GameFiles.Tools.Ddt
{
    public static class DdtFileUtils
    {
        public static void Ddt2Png(string ddtFile)
        {
            var outname = ddtFile.ToLower().Replace(".ddt", ".png");
            if (File.Exists(outname))
                File.Delete(outname);

            var dtdFile = new DdtFile(File.ReadAllBytes(ddtFile));
            dtdFile.SaveAsPng(outname);
        }
    }
}