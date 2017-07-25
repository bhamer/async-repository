## Revisiting the Repository and Unit of Work patterns in an asynchronous world
In this article I'll be introducing a Repository and Unit of Work design that addresses several shortcomings of a design I've both used and seen regularly in the wild (search phrases like "EF base repository" or "C# generic repository" for examples).

But first... let's review the [purpose](http://www.asp.net/mvc/overview/older-versions/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application) of the Repository and Unit of Work patterns:

> The repository and unit of work patterns are intended to create an abstraction layer between the data access layer and the business logic layer of an application. Implementing these patterns can help insulate your application from changes in the data store and can facilitate automated unit testing.

To get a bit a more specific, the Repository pattern decouples the business logic layer from the data access layer by hiding the data access implementation details from the users of that data; the Unit of Work pattern provides a way of keeping track of logical transactions and, when it's time to save, translating those transactions to all-or-nothing state changes to the underlying data store.

So now that we've got that thorough review under our belts, let's take a look at the common Repository and Unit of Work design I'm referencing and follow that up with my case for why it has some weaknesses and how they can be addressed.

### The incumbent
A common (and perfectly valid) implementation of the Repository and Unit of Work patterns begins by defining and implementing a repository interface per domain type. E.g. IPositionsRepository for a Position type and ITradesRepository for a Trade type. Each repository will have a constructor that takes in a DbContext (or some other ORM construct) as a parameter. The DbContext can then be shared across repositories or a unique DbContext can be passed into each repository.

Here’s a look at that pattern:
```csharp
public interface IPositionRepository
{
    Task<IEnumerable<Position>> GetPositionsForAccountAsync(string accountCode, DateTime positionDate);
    void Add(Position position);
    void Remove(Position position);
    // additional repository methods..
}

public class PositionRepository : IPositionRepository
{
    private readonly DbContext context;
    public PositionRepository(DbContext context)
    {
        this.context = context;
    }

    // IPositionRepository implementation..
}
```

And it's common for a generic base repository to be added as well, which looks something like this:
```csharp
public interface IBaseRepository<T> where T : class
{
    void Add(T entity);
    void Remove(T entity);
    // additional generic CRUD methods..
}

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    private readonly DbSet<T> dbSet;
    public BaseRepository(DbContext context)
    {
        dbSet = context.Set<T>();
    }

    public void Add(T entity)
    {
        dbSet.Add(entity);
    }

    // IBaseRepository<T> implementation..
}

public interface IPositionRepository : IBaseRepository<Position>
{
    Task<IEnumerable<Position>> GetPositionsForAccountAsync(string accountCode, DateTime positionDate);
    // additional methods specific to Position domain type..
}

public class PositionRepository : BaseRepository<Position>, IPositionRepository
{
    private readonly DbContext context;
    public PositionRepository(DbContext context) : base(context)
    {
        this.context = context;
    }

    // IPositionRepository implementation..
}
```


Here are the issues I've encountered using this design.

##### Not async-friendly
This one is best shown through an example. Let's say we have a repository that we need to query twice within a method. For instance, a service that calculates the daily gain or loss for an account. To do this calculation we'll need positions for the day we care about and positions from the previous day.

The service would have a reference to a PositionRepository and the method might look something like this:
```csharp
public async Task<decimal> CalculateDailyGainLoss(string accountCode, DateTime positionDate)
{
	// get T and T-1 positions
	var todaysPositionsTask = positionRepo.GetPositionsForAccountAsync(accountCode, positionDate.Date);            
	var yesterdaysPositionsTask = positionRepo.GetPositionsForAccountAsync(accountCode, positionDate.AddDays(-1).Date);

	// wait for both methods to complete asynchronously
	await Task.WhenAll(todaysPositionsTask, yesterdaysPositionsTask); // exception thrown here

	// calculate day over day GL using today's and yesterday's positions
	return todaysPositionsTask.Result.Sum(p => p.MarketValue) - yesterdaysPositionsTask.Result.Sum(p => p.MarketValue);            
}
```

The problem here is that our unit of work (DbContext) is not thread-safe. So when the repository methods are run concurrently an exception will be thrown by the DbContext. This is bad. It forces us to run those methods synchronously when we could have taken advantage of the async constructs to run them concurrently.

And yes, I realize the GetPositionsForAccountAsync method could have a date range as it's parameter and our problem would be solved. But this was a contrived example to prove a point so bear with me.

##### Constructor injection explosion
Being experienced object-oriented programmers, we have our business layer depend on abstractions by passing in repository interfaces as parameters to our services, controllers, etc.. 

This means if a service is performing operations on multiple domain types, a repository for each of those types needs to be passed in as a parameter.

Does this constructor signature look familiar?
```csharp
public class PositionService
{
	// constructor signature
	public PositionService(IPositionRepository positionRepo, ITradeRepository tradeRepo, IAccountRepository accountRepo, etc...)

	public void OpenPosition(string accountCode, Trade trade)
	{
		// verify the account is active
		if (accountRepo.Find(accountCode) == null) throw new ArgumentException("bad account code");

		// store trade
		tradeRepo.Add(trade);

		// create position
		var position = new Position() { AccountCode = accountCode, PositionDate = trade.TradeDate, MarketValue = trade.MarketValue };
		positionRepo.Add(position);

		// save changes to data store
		// Example of multi-transaction save antipattern described in "Broken unit of work" section below
		tradeRepo.SaveChanges();
		positionRepo.SaveChanges();       
	}
}
```

In this example, the PositionService constructor requires three repository parameters to implement its OpenPosition method, which isn't too bad. But I've seen the parameter count go past the ten mark several times. That's when it starts to get ugly.

But let's forget about the subjective attractiveness of the code. This also means a repository needs to be added to the constructor every time a new domain type is required, which is extra work - and who likes extra work? This also becomes a hassle in unit testing. Whenever a new repository is added to the service, it means the service's unit tests will all break until the new repository is mocked up and injected.

##### Broken unit of work
This one isn't necessarily an issue with the incumbent design, itself. It's an issue with a particular variant of the design that I've seen enough times that it's worth mentioning.

The offending design is to pass in a unique unit of work (e.g. DbContext) to each repository and have a SaveChanges() method implemented for each repository. The SaveChanges() method is typically implemented in a generic base repository.

By using this design, the benefits of the unit of work pattern are greatly diminished. You still have a unit of work per domain type but as soon as two or more domain types are part of a single logical business transaction the unit of work becomes broken because you end up having to call SaveChanges() on multiple repositories. This defeats the all-or-nothing behavior of the unit of work pattern by saving components of the business transaction to the database in separate transactions.

The easiest way to spot a broken unit of work is the SaveChanges() method being called by multiple repositories in a single method. See example above in OpenPosition method.

##### Leaking the ORM
This one isn't limited to any specific implementation of the repository pattern but it's something I see quite often when Entity Framework is being used.

Just search "C# generic base repository" and you'll see how prevalent this design is. Here's what I'm talking about:
```csharp
public interface IBaseRepository<T> where T : class
{
	IQueryable<T> GetAll();
	// additional generic CRUD methods..
}
```

My problem with this is the IQueryable being returned from the GetAll() method. And here's why: 
> An IQueryable provides functionality to evaluate queries against a *specific data source* [emphasis added]. 

That's essentially saying an IQueryable behaves differently based on the underlying data source. So if you're using an IQueryable you need to know what that specific data source is in order to use the IQueryable appropriately. And the whole point of the repository pattern is to abstract away the DAL so the business layer doesn't have to know or care about the underlying implementation details. This is a leaky abstraction.

And if that isn't a good enough reason to not return IQueryable. Another side effect of returning an IQueryable is that if you ever wanted to switch out your ORM (let's say from EF to Dapper .NET) you'd have to implement the IQueryable interface and ensure it behaves exactly the same as EF's implementation. As fun as that sounds, I don't think it's a particularly valuable use of time if there's a deadline looming (and when isn't there?).

To summarize, returning an IQueryable couples our repository to a specific ORM implementation. So if we want a truly ORM-agnostic repository, we should avoid returning IQueryable from our repository methods.

So those are the problems I have with the repository and unit of work design I've described above. Now I'll discuss my proposed alternative.

### An async-friendly, ORM-agnostic Repository and Unit of Work design
First off, I want to throw out the disclaimer that I'm not claiming to have come up with anything particularly unique here. This is really just an amalgamation of concepts I liked while researching alternatives to the design I described above and some trial and error using this design in a real project. So insert some "standing on the shoulders of giants" quote here.

Let's start off by addressing the async-friendly requirement. To do this, I utilized the [CQRS](http://martinfowler.com/bliki/CQRS.html) pattern.

The way I integrated the CQRS pattern into the repository design is by splitting my repositories into two distinct categories:
- Query Repositories: read-only, thread-safe, repos that do not result in any state changes
- Command Repositories: repos that result in state changes

Let's focus on the query repositories first as that's the async-friendly part of the overall pattern.

##### Query Repositories
I went back and forth between these two methods of implementation:
- Define a query interface per domain type (e.g. for a Position type there would be an IPositionQueryRepository)
- Create a generic query interface and add type-specific extension methods to that interface (e.g. have an IQueryRepository\<T\> and create extension methods like this GetPositionsAsync(this IQueryRepository\<Position\> repo, DateTime positionDate))

I picked the first option for a couple reasons. First, it's simpler to understand when you look at it. Second, and more importantly IMO, mocking repository methods for unit testing isn't naturally supported using extension methods.

Here's a look at a query repository:
```csharp
public class PositionQueryRepository : IPositionQueryRepository
{
	private readonly string connectionString;
	
	public PositionQueryRepository(string connectionString)
	{
		this.connectionString = connectionString;
	}

	// using Dapper.NET
	public async Task<IEnumerable<Position>> GetPositionsForAccountAsync(string accountCode, DateTime positionDate)
	{
		using (var conn = new SqlConnection(connectionString))
		{
			return await conn.QueryAsync<Position>("select * from Position where AccountCode = @AccountCode and PositionDate = @PositionDate", 
				new { AccountCode = accountCode, PositionDate = positionDate }).ConfigureAwait(false);
		}
	}

	// using EF6
	public async Task<List<Position>> EF_GetPositionsForAccountAsync(string accountCode, DateTime positionDate)
	{
		using (var context = new MyDbContext(connectionString))
		{
			return await context.Positions.Where(p => p.AccountCode == accountCode && p.PositionDate == positionDate)
				.ToListAsync().ConfigureAwait(false);
		}
	}
}
```

Notice the query repo is ORM-agnostic. In fact, I'm using Dapper.NET and EF6 to do the same thing in the repo just to prove it to you. This makes it easy to use the ORM best suited to solve the problem at hand instead of being locked into one for every situation. For example, Dapper could be used for high performance fetches while EF is used for complex navigation scenarios.

Going back to my earlier example of the gain or loss calculation service, I can now successfully use C# async constructs to query the position repository concurrently:
```csharp
public async Task<decimal> CalculateDailyGainLoss(string accountCode, DateTime positionDate)
{
	// get T and T-1 positions
	var todaysPositionsTask = positionQueryRepo.GetPositionsForAccountAsync(accountCode, positionDate.Date);            
	var yesterdaysPositionsTask = positionQueryRepo.GetPositionsForAccountAsync(accountCode, positionDate.AddDays(-1).Date);

	// wait for both methods to complete asynchronously
	await Task.WhenAll(todaysPositionsTask, yesterdaysPositionsTask).ConfigureAwait(false);

	// calculate day over day GL using today's and yesterday's positions
	return todaysPositionsTask.Result.Sum(p => p.MarketValue) - yesterdaysPositionsTask.Result.Sum(p => p.MarketValue);            
}
```

Alright, let's move on to the command repositories. 

##### Command Repositories
The ICommandRepository\<T\> interface will be acting as the mediator between the data access layer and the business layer for any state-change operations.
```csharp
public interface ICommandRepository<T> where T : class
{
	T Find(params object[] keyValues);
	void Add(T entity);
	void AddRange(IEnumerable<T> entities);
	void Remove(T entity);
	void RemoveRange(IEnumerable<T> entities);
	void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
}
```

The first thing to notice about the ICommandRepository\<T\> is that it doesn't have a Commit or SaveChanges method. Remember, changes are saved to a unit of work, not a repository. So the repositories will not have save methods; they'll operate on a unit of work, which has a save method.

To implement the ICommandRepository\<T\> interface, I'll define a generic class that implements all the methods with the help of Entity Framework 6. It's important to note that, while I'm using EF6, the interface could be implemented using any other ORM out there.
```csharp
public class EFCommandRepository<T> : ICommandRepository<T> where T : class
{
	private readonly DbContext dbContext;
	private readonly DbSet<T> dbSet;

	public EFCommandRepository(DbContext dbContext)
	{
		this.dbContext = dbContext;
		dbSet = dbContext.Set<T>();
	}

	public virtual T Find(params object[] keyValues)
	{
		return dbSet.Find(keyValues);
	}

	public virtual void Add(T entity)
	{
		if (entity == null) throw new ArgumentNullException(nameof(entity));
		dbSet.Add(entity);
	}
	
	// the rest of ICommandRepository<T> implementation...
}
```

Next up is the unit of work design. For a class to function as a unit of work, it must be able to commit changes to a data store as a single transaction. So I defined a unit of work interface to capture that functionality.
```csharp
public interface IUnitOfWork : IDisposable
{
	void Commit();
	Task CommitAsync();
}
```

And let's say my data store is a SQL Server database with three tables named Account, Position, and Trade. So I'll define a unit of work to represent this database.
```csharp
// ORM-agnostic unit of work
public interface IMyUnitOfWork : IUnitOfWork
{
	ICommandRepository<Account> AccountRepository { get; }
	ICommandRepository<Position> PositionRepository { get; }
	ICommandRepository<Trade> TradeRepository { get; }
}
```

As you can see, an ICommandRepository\<T\> interface is added for each domain type. The purpose of having the unit of work interface expose command repositories for all the domain types is that it will allow my business layer to do state-change operations on multiple domain types, through their respective repos, and then commit all the changes as a single transaction.

Now to implement the IMyUnitOfWork interface. To do this, I'm going to use EF's DbContext to help me out because it's a perfectly acceptable Unit of Work implementation; and why reinvent the wheel? So I'll start by defining a DbContext that will act as my ORM on top of the database.
```csharp
public class MyDbContext : DbContext
{
	public DbSet<Account> Accounts { get; set; }
	public DbSet<Position> Positions { get; set; }
	public DbSet<Trade> Trades { get; set; }

	public MyDbContext(string connectionString) 
		: base(connectionString) { }
}
```

And now I'll implement IMyUnitOfWork by inheriting from MyDbContext and using my EFCommandRepository\<T\> class as the implementation for the command repositories. You'll notice I'm using the Lazy\<T\> type to wrap the command repos. This will make dependency injection into services and controllers more efficient because no command repos will be initialized until they're actually needed.
```csharp
public class MyUnitOfWork : MyDbContext, IMyUnitOfWork
{
	public MyUnitOfWork(string connectionString)
		: base(connectionString)
	{
		accountRepo = new Lazy<ICommandRepository<Account>>(() => new EFCommandRepository<Account>(this));
		positionRepo = new Lazy<ICommandRepository<Position>>(() => new EFCommandRepository<Position>(this));
		tradeRepo = new Lazy<ICommandRepository<Trade>>(() => new EFCommandRepository<Trade>(this));
	}

	private readonly Lazy<ICommandRepository<Account>> accountRepo;
	public ICommandRepository<Account> AccountRepository
	{
		get { return accountRepo.Value; }
	}

	private readonly Lazy<ICommandRepository<Position>> positionRepo;
	public ICommandRepository<Position> PositionRepository
	{
		get { return positionRepo.Value; }
	}

	private readonly Lazy<ICommandRepository<Trade>> tradeRepo;
	public ICommandRepository<Trade> TradeRepository
	{
		get { return tradeRepo.Value; }
	}

	public void Commit()
	{
		SaveChanges();
	}

	public Task CommitAsync()
	{
		return SaveChangesAsync();
	}
}
```

Here's a look at how this unit of work implementation could be used by the position service:
```csharp
public class PositionService
{
	private readonly IMyUnitOfWork myUnitOfWork;

	public PositionService(IMyUnitOfWork myUnitOfWork)
	{
		this.myUnitOfWork = myUnitOfWork;        
	}
	
	public void OpenPosition(string accountCode, Trade trade)
	{
		// verify the account exists
		if (myUnitOfWork.AccountRepository.Find(accountCode) == null) throw new ArgumentException("bad account code");

		// add trade
		myUnitOfWork.TradeRepository.Add(trade);

		// create and add position
		var position = new Position() { AccountCode = accountCode, PositionDate = trade.TradeDate, MarketValue = trade.MarketValue };
		myUnitOfWork.PositionRepository.Add(position);

		// save changes to data store in a single transaction
		myUnitOfWork.Commit();
	}
}
```

First take a look at the service's constructor and how the repository dependencies are all encapsulated within that single unit of work, as opposed to having to pass in every single repository dependency separately like in the constructor injection explosion example. Since this service has a reference to the unit of work, it can use any command repository. And at the same time, no repository will be initialized unless it's actually used by the service because of the lazy loading.

The second thing to observe is the unit of work pattern being utilized in the OpenPosition method. The method is making updates to multiple repositories and then committing the changes as a single transaction to the database. No broken unit of work here.

### Conclusion
In this article we examined an async-friendly, ORM-agnostic Repository and Unit of Work design that addresses several shortcomings of a design I've both used and seen regularly in the wild. The design isn't meant to be a silver bullet that should be used in all cases. My objective was to solve a particular set of problems while remaining true to the purpose of the Repository and Unit of Work patterns.
