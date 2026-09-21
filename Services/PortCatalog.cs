using PortBan.Models;

namespace PortBan.Services;

internal static class PortCatalog
{
    private readonly record struct Rule(string Keyword, string Label, bool Generic);

    private static readonly (string Keyword, string Label)[] CommandRules =
    [
        ("text-generation-webui", "text-generation-webui（AI 文章）"),
        ("stable-diffusion-webui", "Stable Diffusion WebUI（画像生成）"),
        ("oobabooga", "text-generation-webui（AI 文章）"),
        ("open-webui", "Open WebUI（AI チャット）"),
        ("open_webui", "Open WebUI（AI チャット）"),
        ("comfyui", "ComfyUI（画像生成）"),
        ("fooocus", "Fooocus（画像生成）"),
        ("invokeai", "InvokeAI（画像生成）"),
        ("koboldcpp", "KoboldCPP（ローカル LLM）"),
        ("sillytavern", "SillyTavern"),
        ("anything-llm", "AnythingLLM"),
        ("anythingllm", "AnythingLLM"),
        ("ollama", "Ollama（AI モデル API）"),
        ("lm studio", "LM Studio（ローカル LLM）"),
        ("lmstudio", "LM Studio（ローカル LLM）"),
        ("lm-studio", "LM Studio（ローカル LLM）"),
        ("localai", "LocalAI"),
        ("vllm", "vLLM（LLM 推論）"),
        ("llama-server", "llama.cpp（LLM 推論）"),
        ("llama.cpp", "llama.cpp（LLM 推論）"),
        ("tensorboard", "TensorBoard"),
        ("streamlit", "Streamlit"),
        ("gradio", "Gradio（AI デモ）"),
        ("jupyter-lab", "JupyterLab"),
        ("jupyter-notebook", "Jupyter Notebook"),
        ("jupyter", "Jupyter"),
        ("mlflow", "MLflow"),
        ("label-studio", "Label Studio"),
        ("next-server", "Next.js（開発サーバー）"),
        ("next/dist", "Next.js（開発サーバー）"),
        ("next dev", "Next.js（開発サーバー）"),
        ("next start", "Next.js（開発サーバー）"),
        ("node_modules/next", "Next.js（開発サーバー）"),
        ("bin/next", "Next.js（開発サーバー）"),
        ("vite", "Vite（開発サーバー）"),
        ("nuxt", "Nuxt（開発サーバー）"),
        ("astro", "Astro（開発サーバー）"),
        ("remix", "Remix（開発サーバー）"),
        ("storybook", "Storybook"),
        ("webpack-dev-server", "webpack（開発サーバー）"),
        ("ng serve", "Angular（開発サーバー）"),
        ("react-scripts", "Create React App（開発サーバー）"),
        ("expo", "Expo（開発サーバー）"),
        ("uvicorn", "Uvicorn（ASGI サーバー）"),
        ("fastapi", "FastAPI"),
        ("gunicorn", "Gunicorn"),
        ("http.server", "Python HTTP サーバー"),
        ("flask", "Flask"),
        ("django", "Django"),
        ("artisan serve", "PHP 開発サーバー"),
        ("microsoft.aspnetcore", "ASP.NET Core"),
        ("code-server", "code-server"),
        ("docker-proxy", "Docker"),
        ("com.docker.backend", "Docker"),
    ];

    private static readonly Rule[] ProcessRules =
    [
        new("ollama", "Ollama（AI モデル API）", false),
        new("lm studio", "LM Studio（ローカル LLM）", false),
        new("lmstudio", "LM Studio（ローカル LLM）", false),
        new("com.docker.backend", "Docker", false),
        new("docker-proxy", "Docker", false),
        new("dockerd", "Docker", false),
        new("postgres", "PostgreSQL", false),
        new("postgresql", "PostgreSQL", false),
        new("mysqld", "MySQL", false),
        new("mariadbd", "MariaDB", false),
        new("redis-server", "Redis", false),
        new("mongod", "MongoDB", false),
        new("sqlservr", "SQL Server", false),
        new("nginx", "nginx", false),
        new("httpd", "Apache httpd", false),
        new("caddy", "Caddy", false),
        new("cursor", "Cursor", false),
        new("code", "VS Code", false),
        new("svchost", "Windows サービスホスト", true),
        new("lsass", "Windows セキュリティ", true),
        new("spoolsv", "印刷スプーラー", true),
        new("wininit", "Windows システム", true),
        new("services", "Windows サービス", true),
        new("system", "Windows システム", true),
        new("systemd", "systemd", true),
    ];

    private static readonly Dictionary<int, string> KnownPorts = new()
    {
        [20] = "FTP データ",
        [21] = "FTP",
        [22] = "SSH",
        [23] = "Telnet",
        [25] = "SMTP",
        [53] = "DNS",
        [67] = "DHCP",
        [68] = "DHCP クライアント",
        [80] = "HTTP",
        [110] = "POP3",
        [111] = "RPCbind",
        [123] = "NTP",
        [135] = "Windows RPC",
        [137] = "NetBIOS 名前",
        [138] = "NetBIOS データグラム",
        [139] = "NetBIOS セッション",
        [143] = "IMAP",
        [161] = "SNMP",
        [389] = "LDAP",
        [443] = "HTTPS",
        [445] = "Windows ファイル共有（SMB）",
        [465] = "SMTPS",
        [500] = "VPN（IKE）",
        [514] = "Syslog",
        [515] = "印刷（LPD）",
        [587] = "メール送信",
        [631] = "印刷（IPP）",
        [993] = "IMAPS",
        [995] = "POP3S",
        [1234] = "LM Studio（よく使うポート）",
        [1433] = "SQL Server",
        [1521] = "Oracle",
        [1883] = "MQTT",
        [1900] = "SSDP",
        [2049] = "NFS",
        [2181] = "ZooKeeper",
        [2375] = "Docker API",
        [2376] = "Docker API（TLS）",
        [2379] = "etcd",
        [3000] = "開発サーバー（よくある既定）",
        [3001] = "開発サーバー",
        [3260] = "iSCSI",
        [3306] = "MySQL",
        [3389] = "リモートデスクトップ",
        [3478] = "STUN",
        [3702] = "WS-Discovery",
        [4000] = "開発サーバー",
        [4040] = "Spark UI",
        [4173] = "Vite プレビュー",
        [4200] = "Angular（よくある既定）",
        [4891] = "GPT4All",
        [5000] = "Web API / Flask",
        [5001] = "KoboldCPP / Web API",
        [5040] = "Windows デバイス接続",
        [5173] = "Vite（開発サーバー）",
        [5353] = "mDNS",
        [5355] = "LLMNR",
        [5357] = "Web Services on Devices",
        [5432] = "PostgreSQL",
        [5433] = "PostgreSQL",
        [5672] = "RabbitMQ",
        [5900] = "VNC",
        [5984] = "CouchDB",
        [5985] = "WinRM",
        [6006] = "TensorBoard",
        [6333] = "Qdrant",
        [6379] = "Redis",
        [6443] = "Kubernetes API",
        [7474] = "Neo4j",
        [7680] = "Windows 配信の最適化",
        [7687] = "Neo4j Bolt",
        [7860] = "Gradio / Stable Diffusion",
        [7861] = "Gradio",
        [7865] = "Fooocus（画像生成）",
        [8000] = "Web API（よくある既定）",
        [8080] = "HTTP 代替 / Web API",
        [8081] = "開発サーバー",
        [8188] = "ComfyUI（画像生成）",
        [8265] = "Ray Dashboard",
        [8443] = "HTTPS 代替",
        [8501] = "Streamlit",
        [8787] = "Dask",
        [8888] = "Jupyter",
        [9000] = "管理画面（よくある既定）",
        [9042] = "Cassandra",
        [9090] = "Prometheus / InvokeAI",
        [9092] = "Kafka",
        [9200] = "Elasticsearch",
        [9300] = "Elasticsearch",
        [9418] = "Git",
        [11211] = "Memcached",
        [11434] = "Ollama（AI モデル API）",
        [15672] = "RabbitMQ 管理画面",
        [19530] = "Milvus",
        [27017] = "MongoDB",
        [51820] = "WireGuard",
    };

    private static readonly (string Keyword, string Label)[] CommandRulesByLength =
        CommandRules.OrderByDescending(rule => rule.Keyword.Length).ToArray();

    private static readonly Rule[] ProcessRulesByLength =
        ProcessRules.OrderByDescending(rule => rule.Keyword.Length).ToArray();

    public static string Describe(
        int port,
        PortProtocol protocol,
        string? processName,
        string? fileDescription,
        string? commandLine)
    {
        var commandPurpose = MatchCommand(commandLine);
        if (commandPurpose is not null)
            return commandPurpose;

        var specificProcess = MatchProcess(processName, generic: false);
        if (specificProcess is not null)
            return specificProcess;

        if (KnownPorts.TryGetValue(port, out var byPort))
            return byPort;

        var genericProcess = MatchProcess(processName, generic: true);
        if (genericProcess is not null)
            return genericProcess;

        var description = CleanDescription(fileDescription, processName);
        if (description is not null)
            return description;

        return Unknown(protocol);
    }

    private static string Unknown(PortProtocol protocol)
    {
        switch (protocol)
        {
            case PortProtocol.Tcp:
            case PortProtocol.Udp:
                return "用途不明";
            default:
                return ThrowUnknown(protocol);
        }
    }

    private static string ThrowUnknown(PortProtocol protocol) =>
        throw new ArgumentOutOfRangeException(nameof(protocol), protocol, null);

    private static string? MatchCommand(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return null;

        var haystack = commandLine.ToLowerInvariant();
        foreach (var (keyword, label) in CommandRulesByLength)
        {
            if (ContainsKeyword(haystack, keyword))
                return label;
        }

        return null;
    }

    private static string? MatchProcess(string? processName, bool generic)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return null;

        var name = Path.GetFileNameWithoutExtension(processName).ToLowerInvariant();
        if (name.Length == 0)
            return null;

        foreach (var rule in ProcessRulesByLength)
        {
            if (rule.Generic != generic)
                continue;

            if (name.Equals(rule.Keyword, StringComparison.Ordinal)
                || name.StartsWith(rule.Keyword + "-", StringComparison.Ordinal)
                || name.StartsWith(rule.Keyword + ".", StringComparison.Ordinal))
                return rule.Label;
        }

        return null;
    }

    private static string? CleanDescription(string? description, string? processName)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var text = description.Trim();
        var baseName = Path.GetFileNameWithoutExtension(processName ?? "");
        if (text.Equals(processName, StringComparison.OrdinalIgnoreCase)
            || (baseName.Length > 0 && text.Equals(baseName, StringComparison.OrdinalIgnoreCase)))
            return null;

        return text.Length > 80 ? text[..80] : text;
    }

    private static bool ContainsKeyword(string haystack, string keyword)
    {
        var index = 0;
        while (index < haystack.Length)
        {
            var found = haystack.IndexOf(keyword, index, StringComparison.Ordinal);
            if (found < 0)
                return false;

            var beforeOk = found == 0 || !IsWord(haystack[found - 1]);
            var end = found + keyword.Length;
            var afterOk = end >= haystack.Length || !IsWord(haystack[end]);
            if (beforeOk && afterOk)
                return true;

            index = found + 1;
        }

        return false;
    }

    private static bool IsWord(char value) => char.IsLetterOrDigit(value);
}
