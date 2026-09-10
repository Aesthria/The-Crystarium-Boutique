@{
    RootFiles = @(
        'CrystariumBoutique.dll'
        'CrystariumBoutique.Core.dll'
        'CrystariumBoutique.deps.json'
        'CrystariumBoutique.json'
    )

    ImageFiles = @(
        'icon.png'
        'equipment-slots.png'
        'previous-item.png'
        'crystal-wardrobe.png'
        'unavailable-dye.png'
        'crystarium-stained-glass.png'
        'crystarium-item-frame-cornered.png'
        'crystarium-gold-frames.png'
        'favorite-badge.png'
    )

    DataFiles = @(
        'item-acquisition-supplement.json'
        'item-availability-supplement.json'
    )

    DistributionFiles = @(
        @{
            Source = 'LICENSE'
            Destination = 'LICENSE'
        }
        @{
            Source = 'COPYRIGHT.md'
            Destination = 'COPYRIGHT.md'
        }
        @{
            Source = 'THIRD-PARTY-NOTICES.md'
            Destination = 'THIRD-PARTY-NOTICES.md'
        }
        @{
            Source = 'tools\CrystariumBoutique.AcquisitionGenerator\supplemental-data\LuminaSupplemental-5.1.4\LICENSE'
            Destination = 'LuminaSupplemental-GPL-3.0.txt'
        }
    )
}
