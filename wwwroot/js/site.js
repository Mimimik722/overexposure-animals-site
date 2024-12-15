ymaps3.import.registerCdn('https://cdn.jsdelivr.net/npm/{package}', [
    '@yandex/ymaps3-default-ui-theme@latest'
]);

async function initMap() {
    await ymaps3.ready;

    const { YMap, YMapDefaultSchemeLayer, YMapDefaultFeaturesLayer} = ymaps3;

    var coords = JSON.parse(coordinates);

    const map = new YMap(
        document.getElementById('map'),
        {
            location: {
                center: [coords.longitude, coords.latitude],
                zoom: 18
            }
        }
    );

    const { YMapDefaultMarker } = await ymaps3.import('@yandex/ymaps3-default-ui-theme');

    const marker = new YMapDefaultMarker(
        {
            coordinates: [coords.longitude, coords.latitude],
            title: "Ваш питомец"
        }
    );


    map.addChild(new YMapDefaultSchemeLayer());
    map.addChild(new YMapDefaultFeaturesLayer());
    map.addChild(marker);
}

initMap();