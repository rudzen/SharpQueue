﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharp.Queue;
using SharpQueue;

namespace Sharp.Queue.Playground {
    class Program {

        public static bool Running = true;
        public static int ItemsLength = 1000;
        public static int EnqueueThreads = 1000;
        public static int DequeueThreads = 1;
        public static object _syncRoot = new object();
        public static long Dequeued;


        static void Main(string[] args) {
            Console.WriteLine("Started");
            var items = CreateItem(ItemsLength).ToList();
            for (int i = 0; i < EnqueueThreads; i++) {
                Task.Run(async () => {
                    var en = new Enqueuer();
                    await en.Enqueue(items);
                });
            }
            for (int i = 0; i < DequeueThreads; i++) {
                Task.Run(async () => {
                    var de = new Dequeuer();
                    await de.Dequeue();
                });
            }
            Task.Run(async () => {
                using (var q = new DirectoryQueue("queue")) {
                    while (Program.Running) {
                        await Task.Delay(2000);
                        var info = q.GetQueueInfo();
                        Console.WriteLine($"-> Queue -> Enqueued: {info.Enqueued} Error: {info.Error} Total: {Program.Dequeued}");
                    }
                }
            });
            StartQueueCleaner();


            Console.WriteLine("Running");
            Console.ReadLine();
            Console.WriteLine("Stopping");
            Running = false;
            Console.ReadLine();
        }

        private static void StartQueueCleaner() {
            Task.Run(async () => {
                using (var q = new DirectoryQueue("queue")) {
                    while (Program.Running) {
                        await Task.Delay(5000);
                        try {
                            q.CleanQueue();
                        }
                        catch (Exception ex) {
                            Console.WriteLine("Error cleaning the queue!");
                        }
                        Console.WriteLine("Queue CLEANED!");
                    }
                }
            });
        }

        private static IEnumerable<Item> CreateItem(int count) {
            for (int i = 0; i < count; i++) {
                yield return new Item { Age = i, Name = "Name " + i };
            }
        }
    }

    public class Item {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class Enqueuer {
        public async Task Enqueue(List<Item> items) {
            using (var q = new DirectoryQueue("queue")) {
                while (Program.Running) {
                    try {
                        q.Enqueue(items);
                    }
                    catch (Exception ex) {
                        Console.WriteLine("Enqueue error: " + ex);
                    }
                    await Task.Delay(5000);
                }
            }
        }
    }

    public class Dequeuer {
        private Random _random = new Random();
        public bool Running { get; set; } = true;

        public async Task Dequeue() {
            using (var q = new DirectoryQueue("queue")) {
                while (Program.Running) {
                    try {
                        var item = q.Dequeue<List<Item>>();
                        lock(Program._syncRoot) {
                            Program.Dequeued++;
                        }
                        await Task.Delay(_random.Next(1, 4) * 1);
                    }
                    catch (Exception ex) {
                        Console.WriteLine("Dequeue error: " + ex);
                    }
                    //Console.WriteLine("Dequeued: " + item ?? "null");
                }
            }
        }
    }
}
