using System;
using UniRx;

public class Test
{
    public static void Main()
    {
        var rp1 = new ReactiveProperty<int>(1);
        var rp2 = new ReactiveProperty<int>(2);
        
        var merged = Observable.Merge(rp1, rp2);
        
        merged.Subscribe(x => Console.WriteLine(x));
    }
}
