using System;
using System.Collections.Generic;

public interface IDtoConvertible<TDto>
{
    TDto ToDto();
    void ApplyDto(TDto dto);
}

public static class DataMapperUtil
{
    #region Dictionary

    public static Dictionary<TKey, TDtoValue> ToDtoDictionary<TKey, TRuntimeValue, TDtoValue>(
        Dictionary<TKey, TRuntimeValue> source)
        where TRuntimeValue : IDtoConvertible<TDtoValue>
    {
        var result = new Dictionary<TKey, TDtoValue>();

        if (source == null)
            return result;

        foreach (var pair in source)
        {
            if (pair.Value == null)
                continue;

            result[pair.Key] = pair.Value.ToDto();
        }

        return result;
    }

    public static void ApplyDtoDictionary<TKey, TRuntimeValue, TDtoValue>(
        Dictionary<TKey, TRuntimeValue> target,
        Dictionary<TKey, TDtoValue> source,
        Func<TDtoValue, TRuntimeValue> createRuntimeValue)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (createRuntimeValue == null)
            throw new ArgumentNullException(nameof(createRuntimeValue));

        target.Clear();

        if (source == null)
            return;

        foreach (var pair in source)
        {
            if (pair.Value == null)
                continue;

            target[pair.Key] = createRuntimeValue(pair.Value);
        }
    }

    #endregion

    #region List

    public static List<TDto> ToDtoList<TRuntime, TDto>(
        List<TRuntime> source)
        where TRuntime : IDtoConvertible<TDto>
    {
        var result = new List<TDto>();

        if (source == null)
            return result;

        foreach (var item in source)
        {
            if (item == null)
                continue;

            result.Add(item.ToDto());
        }

        return result;
    }

    public static void ApplyDtoList<TRuntime, TDto>(
        List<TRuntime> target,
        List<TDto> source,
        Func<TDto, TRuntime> createRuntime)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (createRuntime == null)
            throw new ArgumentNullException(nameof(createRuntime));

        target.Clear();

        if (source == null)
            return;

        foreach (var dto in source)
        {
            if (dto == null)
                continue;

            target.Add(createRuntime(dto));
        }
    }

    #endregion
}