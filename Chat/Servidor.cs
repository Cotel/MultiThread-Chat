using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Chat {
    class Servidor {
        private static TcpListener serverSocket = default(TcpListener);
        private static Socket clientSocket = default(Socket);

        private static readonly int maxClientsCount = 20;
        private static readonly handleClient[] clients = new handleClient[maxClientsCount];

        static void Main(string[] args) {
            
            serverSocket = new TcpListener(IPAddress.Any, 7777);
            clientSocket = default(Socket);
            serverSocket.Start();
                        
            while (true) {
                Console.WriteLine("Esperando a conexion...");                
                clientSocket = serverSocket.AcceptSocket();
                Console.WriteLine("Conexion!");
                int i = 0;
                for (i = 0; i < maxClientsCount; i++) {
                    if (clients[i] == null) {
                        (clients[i] = new handleClient()).startClient(clientSocket, clients);
                        break;
                    }
                }

                if (i== maxClientsCount) {
                    StreamWriter ots = new StreamWriter(new NetworkStream(clientSocket));
                    ots.AutoFlush = true;
                    ots.WriteLine("*** Servidor lleno ***");
                    ots.Close();
                    clientSocket.Close();
                }
            }
        }
    }

    public class handleClient {
        private Socket clientSocket;
        private handleClient[] clients;
        private int maxClientsCount;
        private String clientName;
        private StreamReader ins;
        private StreamWriter ots;

        public void startClient(Socket inClientSocket, handleClient[] clients) {
            this.clientSocket = inClientSocket;
            this.clients = clients;
            this.maxClientsCount = clients.Length;

            Thread ctThread = new Thread(doChat);
            ctThread.Start();
        }

        private Boolean checkCorrect(String s) {
            if (s.Equals("") || s.Equals("\n")) {
                return false;
            }

            for (int i = 0; i < s.Length; i++) {
                if (!char.IsLetterOrDigit(s.ElementAt(i))) {
                    return false;
                }
            }

            return true;
        }

        private Boolean checkCommand(String s) {
            if (s.Equals("/list") || s.Equals("/quit") || s.Equals("") || s.Equals("\n")) {
                return true;
            }

            return false;
        }

        private void doChat() {
            int maxClientsCount = this.maxClientsCount;
            handleClient[] clients = this.clients;

            try {
                ins = new StreamReader(new NetworkStream(clientSocket));
                ots = new StreamWriter(new NetworkStream(clientSocket));
                ots.AutoFlush = true;
                String name;
                do {
                    ots.WriteLine("*** Introduce tu nombre ***");
                    name = ins.ReadLine().Trim();
                    if (checkCorrect(name)) { break; } else {
                        ots.WriteLine("*** El nombre no puede contener caracteres especiales ***");
                        name = null;
                    }

                } while (true);

                // Bienvenida al usuario
                Console.WriteLine("Nuevo usuario: " + name);
                ots.WriteLine("*** Hola " + name + " ***\n*** Para salir escribe /quit en una nueva linea ***");
                ots.WriteLine("*** Para ver los usuarios conectados escribe /list ***");
                lock(this) {
                    for(int i=0; i<maxClientsCount; i++) {
                        if(clients[i] != null && clients[i] == this) {
                            clientName = "@" + name;
                            break;
                        }
                    }
                    // Info al resto de usuarios
                    for(int i=0; i<maxClientsCount; i++) {
                        if(clients[i] != null && clients[i] != this) {
                            clients[i].ots.WriteLine("*** Un nuevo usuario ha entrado: " + name + " ***");
                        }
                    }
                }

                // Comprobacion de un mensaje
                while(true) {
                    String line = ins.ReadLine();
                    if (line.StartsWith("/quit")) {
                        break;
                    }
                    if(line.StartsWith("/list")) {
                        for(int i=0; i<maxClientsCount; i++) {
                            if(clients[i] != null && clients[i] != this) {
                                ots.WriteLine(clients[i].clientName);
                            }
                        }
                    }
                    if(line.Length < 2) {
                        ots.WriteLine("*** No se pueden enviar mensajes tan cortos ***");
                    }
                    if(line.StartsWith("@")) {
                        String[] words = Regex.Split(line, "\\s");
                        if(words.Length > 1 && words[1] != null) {
                            words[1] = words[1].Trim();
                            if (words[1].Any()) {
                                lock(this) {
                                    for(int i=0; i<maxClientsCount; i++) {
                                        if(clients[i] != null && clients[i] != this
                                            && clients[i].clientName != null
                                            && clients[i].clientName.Equals(words[0])) {
                                            clients[i].ots.WriteLine("<" + name + "> " + words[1]);
                                            this.ots.WriteLine(">" + name + "> " + words[1]);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    } else {
                        lock(this) {
                            if(!checkCommand(line)) {
                                for(int i=0; i<maxClientsCount; i++) {
                                    if(clients[i] != null && clients[i].clientName != null) {
                                        clients[i].ots.WriteLine("<" + name + "> " + line);
                                    }
                                }
                            }
                        }
                    }
                }

                // Salida de usuario
                Console.WriteLine("Usuario " + name + " se deconecta");
                lock(this) {
                    for(int i=0; i<maxClientsCount; i++) {
                        if(clients[i] != null && clients[i] != null) {
                            clients[i].ots.WriteLine("*** Usuario " + name + " ha salido ***");
                        }
                    }
                }
                ots.WriteLine("*** Adios " + name + " ***");

                lock (this) {
                    for(int i=0; i<maxClientsCount; i++) {
                        if(clients[i] == this) {
                            clients[i] = null;
                        }
                    }
                }
                ins.Close();
                ots.Close();
                clientSocket.Close();

            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }

        }
    }
}
