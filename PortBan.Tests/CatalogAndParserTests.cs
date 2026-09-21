using PortBan.Models;
using PortBan.Services;
using Xunit;

namespace PortBan.Tests;

public class CatalogAndParserTests
{
    [Fact]
    public void CommandLineDistinguishesNodeApps()
    {
        var purpose = PortCatalog.Describe(3000, PortProtocol.Tcp, "node", "Node.js JavaScript Runtime", "node /app/node_modules/vite/bin/vite.js");
        Assert.Equal("Vite（開発サーバー）", purpose);
    }

    [Fact]
    public void ShortKeywordDoesNotMatchInsideAnotherWord()
    {
        var purpose = PortCatalog.Describe(54321, PortProtocol.Tcp, "node", "Node.js JavaScript Runtime", "node inviter.js");
        Assert.Equal("Node.js JavaScript Runtime", purpose);
    }

    [Fact]
    public void KnownPortIsUsedForGenericHosts()
    {
        var purpose = PortCatalog.Describe(445, PortProtocol.Tcp, "svchost.exe", "Host Process for Windows Services", null);
        Assert.Equal("Windows ファイル共有（SMB）", purpose);
    }

    [Fact]
    public void AiPortIsRecognizedWithoutProcess()
    {
        var purpose = PortCatalog.Describe(11434, PortProtocol.Tcp, "（不明）", null, null);
        Assert.Equal("Ollama（AI モデル API）", purpose);
    }

    [Fact]
    public void SpecificProcessBeatsGenericPort()
    {
        var purpose = PortCatalog.Describe(3000, PortProtocol.Tcp, "postgres", null, null);
        Assert.Equal("PostgreSQL", purpose);
    }

    [Fact]
    public void UnknownPortWithoutHints()
    {
        var purpose = PortCatalog.Describe(54321, PortProtocol.Udp, "（不明）", null, "");
        Assert.Equal("用途不明", purpose);
    }

    [Fact]
    public void NetstatKeepsListenersAndUdp()
    {
        const string text = """
            Active Connections
              Proto  Local Address          Foreign Address        State           PID
              TCP    0.0.0.0:135            0.0.0.0:0              LISTENING       1260
              TCP    127.0.0.1:3000         127.0.0.1:50000        ESTABLISHED     4000
              TCP    [::]:5173              [::]:0                 LISTENING       2222
              UDP    0.0.0.0:53             *:*                                    1800
              UDP    [::1]:11434            *:*                                    1900
            """;

        var rows = NetstatParser.Parse(text);
        Assert.Equal(4, rows.Count);
        Assert.Contains(rows, row => row.Protocol == PortProtocol.Tcp && row.Port == 135 && row.Pid == 1260 && row.Address == "0.0.0.0");
        Assert.Contains(rows, row => row.Protocol == PortProtocol.Tcp && row.Port == 5173 && row.Address == "::");
        Assert.Contains(rows, row => row.Protocol == PortProtocol.Udp && row.Port == 11434 && row.Address == "::1");
        Assert.DoesNotContain(rows, row => row.Port == 3000);
    }

    [Fact]
    public void ProcNetParsesIpv4AndIpv6Listeners()
    {
        const string tcp = """
              sl  local_address rem_address   st tx_queue rx_queue tr tm->when retrnsmt   uid  timeout inode
               0: 0100007F:170D 00000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 6344 1 00000000c480d334 100 0 0 10 0
               1: 0100007F:0050 00000000:0000 01 00000000:00000000 00:00000000 00000000  1000        0 9999 1 00000000c480d334 100 0 0 10 0
            """;
        const string tcp6 = """
              sl  local_address                         remote_address                        st tx_queue rx_queue tr tm->when retrnsmt   uid  timeout inode
               0: 00000000000000000000000001000000:170D 00000000000000000000000000000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 6345 1 0000000016a36f3c 100 0 0 10 0
               1: 00000000000000000000000000000000:1F90 00000000000000000000000000000000:0000 0A 00000000:00000000 00:00000000 00000000  1000        0 7000 1 0000000016a36f3c 100 0 0 10 0
            """;

        var ipv4 = ProcNetParser.Parse(tcp, PortProtocol.Tcp, ipv6: false);
        var ipv6 = ProcNetParser.Parse(tcp6, PortProtocol.Tcp, ipv6: true);

        var local = Assert.Single(ipv4);
        Assert.Equal("127.0.0.1", local.Address);
        Assert.Equal(5901, local.Port);
        Assert.Equal(6344, local.Inode);

        Assert.Equal(2, ipv6.Count);
        Assert.Contains(ipv6, row => row.Address == "::1" && row.Port == 5901 && row.Inode == 6345);
        Assert.Contains(ipv6, row => row.Address == "::" && row.Port == 8080 && row.Inode == 7000);
    }

    [Fact]
    public void AddressLabelsExposure()
    {
        Assert.Equal("0.0.0.0 · 全公開", AddressText.Format("0.0.0.0"));
        Assert.Equal(":: · 全公開", AddressText.Format("[::]"));
        Assert.Equal("127.0.0.1 · ローカルのみ", AddressText.Format("127.0.0.1"));
        Assert.Equal("::1 · ローカルのみ", AddressText.Format("::1"));
        Assert.Equal("192.168.1.20", AddressText.Format("192.168.1.20"));
    }
}
