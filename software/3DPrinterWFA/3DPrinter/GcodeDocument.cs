using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ControlUSB
{
    public class GcodeDocument
    {
        public string[] Gcodes = new string[] {};
        public bool GcodeCargado = false;

        public GcodeDocument()
        {
        }

		public void Cargar(string documento)
		{            
			try
			{
				Gcodes = File.ReadAllLines(documento);
                GcodeCargado = true;                
			}
			catch (Exception)
			{
				Gcodes = new string[] {};
                GcodeCargado = false;
			}            
        }
       
    }
}
