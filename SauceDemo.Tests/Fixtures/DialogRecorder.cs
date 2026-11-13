namespace SauceDemo.Tests.Fixtures;

/// <summary>
/// Regista os diálogos JavaScript (alert, confirm, prompt) que a página abre e aceita-os,
/// para o teste continuar e poder verificar depois o que apareceu.
/// Sem um handler, o Playwright fecha os diálogos sozinho e o teste nem dá por eles.
/// </summary>
public sealed class DialogRecorder : IDisposable
{
    private readonly IPage _page;
    private readonly List<string> _messages = [];

    public DialogRecorder(IPage page)
    {
        _page = page;
        _page.Dialog += OnDialog;
    }

    public IReadOnlyList<string> Messages
    {
        get
        {
            lock (_messages)
            {
                return [.. _messages];
            }
        }
    }

    public void Dispose() => _page.Dialog -= OnDialog;

    private async void OnDialog(object? sender, IDialog dialog)
    {
        lock (_messages)
        {
            _messages.Add($"{dialog.Type}: {dialog.Message}");
        }

        try
        {
            await dialog.AcceptAsync();
        }
        catch (PlaywrightException)
        {
            // A página fechou antes de o diálogo ser aceite: não há nada a fazer.
        }
    }
}
