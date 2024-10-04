using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsultaCnpjReceita.Model
{
    public class Estoque
    {
        public Estoque() { }

        public Estoque(int id, string docCedente, string docSacado)
        {
            Id = id;
            DocCedente = docCedente;
            DocSacado = docSacado;
        }

        public int Id { get; set; }
        public string DocCedente { get; set; }
        public string DocSacado { get; set; }
    }
}