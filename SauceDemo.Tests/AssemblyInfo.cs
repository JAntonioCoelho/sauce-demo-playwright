// Fixtures correm em paralelo entre si; os testes de uma mesma fixture correm em sequência,
// porque o PageTest guarda Page/Context em propriedades da instância da fixture.
[assembly: Parallelizable(ParallelScope.Fixtures)]
