using System;
using System.Collections.Generic;

/// <summary>
/// Fase 1c texts (first draft written on 12/11/2026 for the developer to revise; the voice belongs to him).
/// Ico: smug and sarcastic, but supportive. The Desconhecido: an NPC the protagonist started and never finished —
/// the player knows it, he does not. Each gift handed over plays the next conversation, once.
/// Keys are persisted in <see cref="MetaProgressProfile.seenDialogues"/> — never rename.
/// </summary>
public static class NexusDialogues
{
    /// <summary>Key of Ico's conversation after she has received <paramref name="giftsGiven"/> gifts (0 = the free first talk).</summary>
    public static string IcoKey(int giftsGiven) => "ico.presente." + giftsGiven;
    public static string UnknownKey(int giftsGiven) => "desconhecido.presente." + giftsGiven;

    private static readonly string[][] IcoLines =
    {
        // 0 — level 1, free: first conversation.
        new[]
        {
            "Ah, então você resolveu parar pra conversar. Milagre.",
            "Eu sou a Ico. Sim, a coisa grudada no seu braço. De nada pelo boxe, aliás.",
            "Não faço ideia de como vim parar aqui. Você também não. Formamos uma bela dupla de perdidos.",
            "Se achar alguma coisa esquisita e brilhante lá fora, traz pra mim. Eu gosto de coisas esquisitas. Olha com quem eu ando.",
        },
        // 1 — level 2.
        new[]
        {
            "Isso é… pra mim? Hm. Ok. Não vou chorar, eu nem tenho olhos.",
            "Dá pra sentir alguma coisa aí dentro. Parece um pedaço de memória que alguém esqueceu de apagar.",
            "Toma, fica com a minha Faixa de Sparring. E eu lembrei de um golpe que estava guardando pra depois. Depois chegou.",
        },
        // 2 — level 3.
        new[]
        {
            "Outro? Você está me mimando. Continue.",
            "Sabe o que me incomoda? Eu sei lutar boxe, mas não lembro de ter aprendido. É como se alguém tivesse escrito isso em mim.",
            "…Enfim. Mais um truque novo pra você. Tenta não errar.",
        },
        // 3 — halfway to level 4.
        new[]
        {
            "Guardei. Ainda não é o bastante pra abrir o que eu estou tentando abrir, mas chega perto.",
            "Traz mais um e eu te mostro uma coisa.",
        },
        // 4 — level 4 (the homage: a castle, and someone led by the hand).
        new[]
        {
            "Pronto. Lembrei de uma coisa: um castelo enorme, um lugar de onde ninguém saía sozinho.",
            "Alguém me levava pela mão o caminho inteiro. Não sei se era eu que segurava ou se era eu a segurada.",
            "Deve ser por isso que eu grudei na sua. Hábito.",
            "Fica com o Protetor Bucal. Você apanha bastante; pelo menos apanhe com estilo.",
        },
        // 5 — halfway to level 5.
        new[]
        {
            "Mais um. Você percebeu que não precisava, né? Eu já ia continuar aqui de qualquer jeito.",
            "Falta pouco pra eu te dar a melhor coisa que eu tenho.",
        },
        // 6 — level 5, signature artifact.
        new[]
        {
            "Tá bom. Chega de enrolar.",
            "Eu não sei se a gente volta pra casa. Nem sei se a minha casa é a mesma que a sua.",
            "Mas enquanto você estiver de pé, eu estou de pé. E se você cair… eu te levanto. Uma vez. Não abusa.",
            "Segundo Round. Era meu. Agora é seu.",
        },
    };

    private static readonly string[][] UnknownLines =
    {
        // 0 — Nexus, first talk (opens the terminal).
        new[]
        {
            "Religuei este terminal. Ele transforma Fragmentos em melhorias. Não me pergunte como eu sei mexer nisso.",
            "Me chamam de Desconhecido. Não é bem um nome. É o que ficou escrito.",
            "Se encontrar um daqueles objetos raros lá fora, eu aceito. Eles me… completam um pouco.",
        },
        // 1 — first layer: a taste. Hands over the Anzol da Sorte.
        new[]
        {
            "Isso… encaixou em algum lugar. Lembrei de uma coisa: eu gostava de pescar. Ou alguém decidiu que eu ia gostar.",
            "Fica com este anzol. Tem um lago que aparece às vezes entre as salas. Com ele, a sorte morde mais.",
        },
        // 2 — second layer: a function.
        new[]
        {
            "Mais um pedaço. Agora eu sei o que eu era pra ser: um guia. Eu devia explicar este mundo pra quem chegasse.",
            "O problema é que metade das minhas explicações está em branco. Os universos estão se misturando, isso eu sei. O porquê ficou sem preencher.",
        },
        // 3 — third layer: a feeling about the author.
        new[]
        {
            "Engraçado. Com este aqui veio uma sensação: a de que alguém me fazia com cuidado e parou no meio.",
            "Não parece abandono. Parece que o autor caiu no sono em cima do teclado.",
            "…Você faz uma cara estranha quando eu falo isso. Deixa pra lá. Obrigado por me terminar um pouco.",
        },
    };

    // "Conversar" (Etapa 4b): short lines for a chat without a gift. One is picked at random; a few open with the bond.
    private static readonly string[] IcoChat =
    {
        "De novo você? Eu estava quase sentindo sua falta. Quase.",
        "Dica de quem mora no seu braço: desvia antes de apanhar. Funciona melhor nessa ordem.",
        "Se aparecer um lago calmo no meio do caminho, pesca. Às vezes vem coisa boa. Às vezes vem peixe.",
        "Aquele sujeito do terminal me dá arrepios. E eu nem tenho pele.",
        "Eu conto os seus socos, sabia? Não te digo o número pra você não ficar convencido.",
    };
    private static readonly string[] IcoChatClose =
    {
        "Sabe que eu já não me imagino em outro braço? Não espalha.",
        "Quando a gente sair daqui, você me deve um descanso. Uma prateleira bonita. Com vista.",
    };
    private static readonly string[] UnknownChat =
    {
        "Eu fico aqui. É o que eu sei fazer por enquanto.",
        "Tem horas em que eu abro a boca e a frase não vem. Como se faltasse o resto do arquivo.",
        "Os Fragmentos que você traz têm um cheiro familiar. Não sei de onde.",
    };
    private static readonly string[] UnknownChatLayered =
    {
        "Hoje eu acordei sabendo uma palavra nova. Não vou gastar ela agora.",
        "Quanto mais inteiro eu fico, mais eu percebo o que falta. É estranho, mas é melhor do que não perceber.",
    };

    /// <summary>A single chat line from Ico; the closer lines join the pool from bond level 3.</summary>
    public static string IcoChatLine(int level, double roll) => Chat(IcoChat, level >= 3 ? IcoChatClose : null, roll);
    /// <summary>A single chat line from the Desconhecido; he has more to say once at least one layer is back.</summary>
    public static string UnknownChatLine(int layers, double roll) => Chat(UnknownChat, layers >= 1 ? UnknownChatLayered : null, roll);

    private static string Chat(string[] basePool, string[] extra, double roll)
    {
        int count = basePool.Length + (extra?.Length ?? 0);
        int index = Math.Min(count - 1, Math.Max(0, (int)(roll * count)));
        return index < basePool.Length ? basePool[index] : extra[index - basePool.Length];
    }

    public static IReadOnlyList<string> Ico(int giftsGiven) => Pick(IcoLines, giftsGiven);
    public static IReadOnlyList<string> Unknown(int giftsGiven) => Pick(UnknownLines, giftsGiven);

    private static IReadOnlyList<string> Pick(string[][] table, int index) =>
        index >= 0 && index < table.Length ? table[index] : Array.Empty<string>();

    /// <summary>One line the Desconhecido's "file" shows per revealed layer (weapon-card style summary).</summary>
    public static string UnknownLayerSummary(int layers)
    {
        switch (layers)
        {
            case 0: return "Um personagem sem ficha. Só o nome provisório.";
            case 1: return "Gosta de pescar. Ou foi feito para gostar.";
            case 2: return "Era para ser um guia deste mundo.";
            default: return "Alguém o fazia com cuidado, e parou no meio.";
        }
    }
}
