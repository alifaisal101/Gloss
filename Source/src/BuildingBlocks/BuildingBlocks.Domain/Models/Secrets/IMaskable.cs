namespace BuildingBlocks.Domain.Models.Secrets;

public interface IMaskable
{
    MaskedSecret Mask();
}