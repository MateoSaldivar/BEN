using UnityEngine;
using System.Collections.Generic;
using System;
/*
* ZiroDev Copyright(c)
*
*/
public class Inventory : MonoBehaviour {
    protected int money;
    public List<Item> items;
    protected int capacity;

    public IReadOnlyList<Item> Items {
        get { return items.AsReadOnly(); }
    }
	public Inventory() {
        capacity = 30;
        items = new List<Item>();
    }

    public Inventory(int capacity) {
        this.capacity = capacity;
        items = new List<Item>();
    }

    public void AddItem(Item item) {
        if (items.Count < capacity) {
            items.Add(item);
        } else {
            Debug.Log("Inventory is full!");
        }
    }

    public void RemoveItem(Item item) {
        items.Remove(item);
    }

    public void AddMoney(int amount) {
        money += amount;
    }

    public void RemoveMoney(int amount) {
        money -= amount;
        if (money < 0) {
            money = 0;
        }
    }
}


[Serializable]
public class Item {
    public int price;
    public string description;
    public Sprite sprite;
    public string name;

    public Item(int price, string description, Sprite sprite, string name) {
        this.price = price;
        this.description = description;
        this.sprite = sprite;
        this.name = name;
    }

    public virtual void Use() {
        Debug.Log("Item used: " + name);
    }
}

[Serializable]
public class Food : Item {
    public float saturation;

    public Food(int price, string description, Sprite sprite, string name, float saturation)
        : base(price, description, sprite, name) {
        this.saturation = saturation;
    }

    public override void Use() {
        Debug.Log("Ate food: " + name + ", restored " + saturation + " health.");
    }
}