using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsultaCnpjReceita.Model
{
    public class Estoque
    {
        public Estoque() { }

        public Estoque(string docCedente, string docSacado)
        {
            Id = Guid.NewGuid();
            DocCedente = docCedente;
            DocSacado = docSacado;
        }

        public Guid Id { get; set; }
        public string DocCedente { get; set; }
        public string DocSacado { get; set; }
    }
}