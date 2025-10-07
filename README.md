## Handling Evolving Entity Framework Models with Cosmos DB

As a product evolves, it’s inevitable that your data model — and by extension, your database contracts — will change over time to include new fields or columns.

When using **Azure Cosmos DB** with **Entity Framework Core** to manage reads and writes through mapped classes, this evolution can lead to issues with older documents. Specifically, previously persisted records won’t contain the new fields introduced in later versions of your model.

Because **EF Core does not call the class’s default or parameterless constructor** when materializing entities, any newly added properties that don’t exist in older records will be set to `null` — regardless of the property’s nullability.

To address this behavior, developers must ensure that new properties are properly initialized, either:

* **At read time**, immediately after entities are retrieved from Cosmos, or
* **Before persistence**, when updating or writing back to the database.

This project demonstrates a simple approach to solving this problem. It introduces a `NullPropertyHydrator` utility class that scans any object — including nested or embedded types — for `null` properties. When a null property is found, it’s automatically hydrated with an appropriate default value, whether it’s a value type, collection, or reference type, independent of its declared nullability.

You can see this approach in action in [`src/NullHydration/Program.cs`](src/NullHydration/Program.cs), which shows how to use the hydrator to ensure all entity properties are fully initialized.


## ChatGPT Description of Entity Framework Entity Materlialization

---

### 🧩 1. How EF Core materializes entities

When EF Core retrieves a document from Cosmos DB and maps it to a CLR entity type:

* **EF does *not* call your class’s constructor** in the usual sense.
  It uses a **low-level materialization process** that bypasses normal construction — it allocates an instance and then sets properties directly via reflection or generated IL.

* This means any **initialization logic or default values** you set in your class constructor **will not run** during deserialization unless EF explicitly uses a constructor with parameters (see below).

---

### 🏗️ 2. What happens to null collections

If your class defines a collection property but the document in Cosmos DB does **not include that field**, then:

* The property will be **set to `null`**, *not* to an empty list.
* EF won’t automatically create an empty collection unless the property is initialized in a constructor or with a default value at declaration.

Example:

```csharp
public class Customer
{
    public string Id { get; set; }

    // Collection navigation
    public List<Order> Orders { get; set; }  // No default initializer
}
```

If the Cosmos document looks like:

```json
{ "id": "1", "name": "Dan" }
```

Then after EF loads it:

```csharp
customer.Orders == null
```

If you want to ensure it’s never null, you should initialize it:

```csharp
public List<Order> Orders { get; set; } = new();
```

or use a constructor and mark EF to use it (see next section).

---

### ⚙️ 3. When EF *does* call a constructor

If you define a constructor with parameters **that match mapped properties**, EF Core may use that constructor instead of the default parameterless one.

For example:

```csharp
public class Customer
{
    public string Id { get; }
    public List<Order> Orders { get; }

    public Customer(string id, List<Order> orders)
    {
        Id = id;
        Orders = orders ?? new();
    }
}
```

EF Core (starting in v5) supports **constructor binding** for such cases — it will call this constructor with values from the document.

But if you only have a **parameterless constructor**, EF skips it and uses its internal materialization path (bypassing constructors).

---

### 🧠 Summary

| Aspect                          | Behavior                                                                         |
| ------------------------------- | -------------------------------------------------------------------------------- |
| Constructor called?             | ❌ Not for parameterless; bypassed by EF materializer                             |
| Collections missing in document | Set to `null`                                                                    |
| To ensure non-null collections  | Initialize inline or use constructor binding                                     |
| Default field initialization    | ✅ Works if done inline (e.g., `= new();`), because EF sets after object creation |

---

### ✅ Best Practice

For Cosmos + EF Core entities, always ensure collections are initialized at declaration:

```csharp
public List<Order> Orders { get; set; } = new();
```

This keeps the entity safe whether loaded by EF, created manually, or serialized/deserialized elsewhere.

---

Would you like me to show an example of how a constructor-binding version behaves versus the reflection-based hydration (so you can see the difference in action)?

