using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ConsultaCnpjReceita.Model;

public class FundoDetalhes
{
    [JsonProperty("id")]
    public Guid id { get; set; }
    [JsonProperty("cnpj_fundo")]
    public string cnpj_fundo { get; set; }
}