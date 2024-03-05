using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Permissions;

namespace INFOMSMC_Block_Relocation
{
    public class GreedyHeuristic
    {
        public Intermediate Intermediate;
        public SortedStructure Sorted;
        public Item[] Items;
        public Stack[] Stacks;
        public GreedyHeuristic()
        {
        }
        public void LoadProblem(Intermediate intermediate)
        {
            this.Intermediate = intermediate;
            this.Sorted = new SortedStructure();
            int N = this.Intermediate.OutputSequenceSize;
            this.Items = new Item[N];
            int id;
            this.Stacks = new Stack[intermediate.Stacks.Count];
            for(int s = 0; s < intermediate.Stacks.Count; s++)
            {
                Stack stack = new Stack(s);
                for (int i = 0; i < intermediate.Stacks[s].Count; i++)
                {
                    id = intermediate.Stacks[s][i];
                    if(id < this.Intermediate.OutputSequenceSize)
                    {
                        this.Items[id] = new Item(id);
                        stack.Push(this.Items[id]);
                    }
                    else{
                        stack.Push(new Item(id));
                    }
                }
                Console.WriteLine(stack.ToString(true));
                this.Stacks[s] = stack;
                if(stack.Count < intermediate.MaxHeight){
                    this.Sorted.Add(stack);
                }
            }
        }
        public int Solve()
        {
            int res = 0;
            Stack s;
            Item block;
            Stack t;
            for(int i = 0; i < this.Items.Length; i++)
            {
                s = this.Items[i].Stack;
                while (s.Top != this.Items[i])
                {
                    block = s.Top;
                    t = this.Sorted.GetClosest(block);
                    s.Pop();
                    t.Push(block);
                    res++;
                    Console.WriteLine(block + " van " + s + " naar " + t);
                    if(t.Count == this.Intermediate.MaxHeight){
                        this.Sorted.Remove(t);
                    }
                    if(s.Count == this.Intermediate.MaxHeight - 1){
                        this.Sorted.Add(s);
                    }
                    Console.WriteLine("New sorted state");
                    foreach (var tttt in this.Sorted.Stacks)
                        Console.WriteLine(tttt.ToString(true));

                    Console.ReadLine();
                }
                s.Pop();
            }
            return res;
        }
    }
    public class SortedStructure
    {
        public List<Stack> Stacks;
        public SortedStructure()
        {
            this.Stacks = new List<Stack>();
        }
        public Stack GetClosest(Item i)
        {
            if (i.Stack == this.Stacks[^1])
                return this.Stacks[^2];
            int start = -1, end = this.Stacks.Count - 1, middle;
            while(end - start > 1)
            {
                middle = (end + start) / 2;
                if (this.Stacks[middle].Count > 0 && this.Stacks[middle].Min < i)
                    start = middle;
                else
                    end = middle;
            }
            return this.Stacks[end];
        }
        public void Update(Stack s)
        {
            int current;
            for (current = 0; current < this.Stacks.Count && this.Stacks[current] != s; current++) ;
            for (current++; current < this.Stacks.Count && this.Stacks[current].Min > s.Min; current++)
                this.Stacks[current - 1] = this.Stacks[current];
            this.Stacks[current - 1] = s;
        }
        public void Add(Stack s){
            this.Stacks.Add(s);
            if(s.Count == 0){
                return;
            }
            int current;
            for(current = this.Stacks.Count - 1; current > 0; current--){
                if(this.Stacks[current - 1].Count > 0 && this.Stacks[current - 1].Top <= s.Top){
                    break;
                }
                this.Stacks[current] = this.Stacks[current - 1];
            }
            this.Stacks[current] = s;
        }
        public void Remove(Stack s){
            this.Stacks.Remove(s);
        }
    }
    public class Stack
    {
        public IList<Item> Items, Minima;
        private int id;
        public Item Top
        {
            get
            {
                return this.Items[^1];
            }
        }
        public Item Min
        {
            get
            {
                if (this.Minima.Count == 0)
                    return Item.MaxValue;
                return this.Minima[^1];
            }
        }
        public int Count{
            get{
                return this.Items.Count;
            }
        }
        public Stack(int id)
        {
            this.Items = new List<Item>();
            this.Minima = new List<Item>();
            this.id = id;
        }
        public void Push(Item i)
        {
            this.Items.Add(i);
            i.Stack = this;
            if (this.Minima.Count == 0)
                this.Minima.Add(i);
            else
            {
                Item min = this.Minima[^1];
                if (i.Id < min.Id)
                    this.Minima.Add(i);
            }
        }
        public Item Pop()
        {
            Item gone = this.Items[^1];
            gone.Stack = null;
            this.Items.RemoveAt(this.Items.Count - 1);
            if (gone == this.Minima[^1])
                this.Minima.RemoveAt(this.Minima.Count - 1);
            return gone;
        }
        public override string ToString()
        {
            return "Stack " + (this.id + 1);
        }

        public string ToString(bool extended)
        {
            return extended ? $"{this.id + 1}:\t{String.Join('\t', this.Items.Select(i => (i.Id + 1) + " "))}" : this.ToString();
        }
    }
    public class Item
    {
        public static Item MaxValue
        {
            get
            {
                return new Item(int.MaxValue);
            }
        }
        public int Id;//Index to the output sequence (so 0 gets picked up first)
        public Stack Stack;
        public Item(int id)
        {
            this.Id = id;
        }
        public static bool operator <(Item i1, Item i2)
        {
            return i1.Id < i2.Id;
        }
        public static bool operator > (Item i1, Item i2)
        {
            return i1.Id > i2.Id;
        }
        public static bool operator <= (Item i1, Item i2)
        {
            return i1.Id <= i2.Id;
        }
        public static bool operator >=(Item i1, Item i2)
        {
            return i1.Id >= i2.Id;
        }
        public override string ToString()
        {
            return "Item " + (this.Id + 1);
        }
        public static bool operator ==(Item i1, Item i2){
            return (i1.Id == i2.Id);
        }
        public static bool operator !=(Item i1, Item i2){
            return (i1.Id != i2.Id);
        }
    }
}