# SuperCache

SuperCache is the most batsh*t insane version of caching that I could think of at the time. I'm not sure if it is actually useful, 
honestly, but it is definitely a thing, because here it is.

The idea is that SuperCache emits some IL to implement an interface for an object that you've passed to it, which is the underlying data source. The interface is implemented more than once, in fact, so that you can purge/expire items from the cache using the same method calls you used to retrieve data into the cache to begin with.

Given you have an interface and implementation like:

```
public interface ISomeInterface
{
  SomeReturnType SomeMethod(int first, string second);
}

public class ImplementationOfISomeInterface : ISomeInterface
{
  public SomeReturnType SomeMethod(int first, string second)
  {
    return new SomeReturnType(first, second);
  }
}
```
  
SuperCache lets you write code like:
  
```
public void MyMethod()
{
  var source = new ImplementationOfISomeInterface();
  var cache = new TransparentCache<ISomeInterface>(source);

  // call SomeMethod with caching
  var result = cache.Cached.SomeMethod(1, "two");

  // expire any result from SomeMethod that may be cached
  cache.Expire.SomeMethod(1, "two");

  // purge any result from SomeMethod that may be cached
  cache.Purge.SomeMethod(1, "two");
  
  // manually insert into the cache
  cache.Insert(new SomeReturnType(11, "twotwo"), i => i.SomeMethod(1, "two"));
}
```
