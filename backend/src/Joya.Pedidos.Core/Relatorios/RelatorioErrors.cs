using Joya.Pedidos.Core.Common;

namespace Joya.Pedidos.Core.Relatorios;

/// <summary>Falhas dos relatórios (catálogo da spec 5.1).</summary>
public static class RelatorioErrors
{
    /// <summary>RN-17: o ranking exige as duas datas. O validator já barra antes; aqui é a defesa do caso de uso.</summary>
    public static Error PeriodoObrigatorio() =>
        Error.Validation(Periodo.CodigoInvalido, "Informe a data inicial e a data final do período.");
}
