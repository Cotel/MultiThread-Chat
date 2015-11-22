using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

class Cliente {
    private static TcpClient client;
    private static StreamReader ins;
    private static StreamWriter ots;
    

    static void Main(string[] args) {
        try {
            client = new TcpClient("127.0.0.1", 7777);
            ins = new StreamReader(client.GetStream());
            ots = new StreamWriter(client.GetStream());
            ots.AutoFlush = true;
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }

        if (client != null && ots != null && ins != null) {
            try {
                cThread cli = new cThread(client, ins, ots);
                Thread ctThread = new Thread(cli.run);
                ctThread.Start();

                while (!cli.closed) {
                    string msg = Console.ReadLine().Trim();
                    ots.WriteLine(msg);
                }
                ots.Close();
                ins.Close();
                client.Close();
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
    }
}

class cThread {

    public bool closed = false;
    private TcpClient client;
    private StreamReader ins;
    private StreamWriter ots;

    public cThread(TcpClient client, StreamReader ins, StreamWriter ots) {
        this.client = client;
        this.ins = ins;
        this.ots = ots;
    } 
    
    public void run() {
        String responseLine;
        try {
            while((responseLine = ins.ReadLine()) != null) {
                Console.WriteLine(responseLine);
                if(responseLine.IndexOf("*** Adios") != -1) {
                    break;
                }
            }
            closed = true;
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
        Environment.Exit(0);
    } 
}