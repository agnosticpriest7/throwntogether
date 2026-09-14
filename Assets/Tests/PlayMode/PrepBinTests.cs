using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ThrownTogether.Tests
{
    public sealed class PrepBinTests
    {
        readonly List<Object> cleanup=new List<Object>();
        T Track<T>(T value) where T:Object {cleanup.Add(value);return value;}
        PrepBin Bin()=>Track(new GameObject("Test bin")).AddComponent<PrepBin>();
        CarrySlot Hands()=>Track(new GameObject("Test hands")).AddComponent<CarrySlot>();
        IngredientDefinition Ingredient()=>Track(ScriptableObject.CreateInstance<IngredientDefinition>());
        Carryable Portion(IngredientDefinition ingredient,CarrySlot owner)
        {
            var item=Track(new GameObject("Test portion")).AddComponent<Carryable>();
            item.visualShader=Shader.Find("Universal Render Pipeline/Lit");
            item.Configure(new ItemPayload{ingredient=ingredient,state=FoodState.Cut});
            owner.TryTake(item);return item;
        }
        [TearDown] public void Cleanup()
        {for(int i=cleanup.Count-1;i>=0;i--)if(cleanup[i]!=null)Object.DestroyImmediate(cleanup[i]);cleanup.Clear();}
        [Test] public void FivePortionsTransferExactlyOnceAndOverflowKeepsOwner()
        {
            var bin=Bin();var hands=Hands();var ingredient=Ingredient();var all=new List<Carryable>();
            for(int i=0;i<5;i++){var item=Portion(ingredient,hands);all.Add(item);Assert.IsTrue(bin.Store(item));Assert.IsNull(hands.Item);}
            var extra=Portion(ingredient,hands);Assert.IsFalse(bin.Store(extra));Assert.AreSame(extra,hands.Item);
            Assert.IsFalse(bin.Take(hands));hands.Release();
            for(int i=4;i>=0;i--){Assert.IsTrue(bin.Take(hands));Assert.AreSame(all[i],hands.Item);Assert.AreSame(hands,hands.Item.Owner);hands.Release();}
            Assert.Zero(bin.Count);Assert.IsFalse(bin.Take(hands));
        }
        [Test] public void MixedRawAndPlatedItemsAreRejectedWithoutLosingThem()
        {
            var bin=Bin();var hands=Hands();var ingredient=Ingredient();
            Assert.IsTrue(bin.Store(Portion(ingredient,hands)));
            var different=Portion(Ingredient(),hands);Assert.IsFalse(bin.Store(different));Assert.AreSame(different,hands.Item);hands.Release();
            var raw=Portion(ingredient,hands);raw.Payload.state=FoodState.Raw;Assert.IsFalse(bin.Store(raw));Assert.AreSame(raw,hands.Item);
            raw.Payload.state=FoodState.Cut;raw.Payload.isPlate=true;Assert.IsFalse(bin.Store(raw));
            raw.Payload.isPlate=false;raw.Payload.additions.Add(new IngredientPortion{ingredient=ingredient,state=FoodState.Cut});Assert.IsFalse(bin.Store(raw));
        }
        [Test] public void EmptyBinCanChangeTypeAndRemovedPortionsDoNotLeavePhantomStock()
        {
            var bin=Bin();var hands=Hands();var a=Ingredient();var b=Ingredient();
            var first=Portion(a,hands);Assert.IsTrue(bin.Store(first));
            Assert.IsTrue(hands.TryTake(first));Assert.Zero(bin.Count);hands.Release();
            Assert.IsTrue(bin.Store(Portion(b,hands)));Assert.AreSame(b,bin.Ingredient);
        }
    }
}
