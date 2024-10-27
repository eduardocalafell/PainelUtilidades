using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsultaCnpjReceita.Model
{
    public class tb_stg_estoque_full
    {
        public tb_stg_estoque_full() { }

        public tb_stg_estoque_full(string docCedente, string docSacado)
        {
            Id = Guid.NewGuid();
            DocCedente = docCedente;
            DocSacado = docSacado;
        }

        public Guid Id { get; set; }
        public string DocCedente { get; set; }
        public string DocSacado { get; set; }
    }

    public class tb_stg_estoque_full_hemera
    {
        public tb_stg_estoque_full_hemera() { }

        public tb_stg_estoque_full_hemera(string docCedente, string docSacado)
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