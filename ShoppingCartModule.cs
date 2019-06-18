using Nancy;
using Nancy.ModelBinding;

namespace ShoppingCart
{
    public class ShoppingCartModule: NancyModule
    {
        private static string productCatalogBaseUrl = 
            @"http://private-05cc8-chapter2productcatalogmicroservice.apiary-mock.com";

        private static string getProductPathTemplate =
            @"/products?productIds=[{0}]";

        private static async Task<HttpResponseMessage> 
            RequestProductFromProductCatalogue(int[] productCatalogueIds)
            {
                var productsResource = string.Format(
                    getProductPathTemplate, string.Join(",", productCatalogueIds)
                );

                using(var httpClient = new HttpClient()){
                    httpClient.BaseAddress = new Uri(productCatalogBaseUrl);

                    return await 
                        httpClient.GetAsync(productsResource).ConfigureAwait(false);
                }
            }


        public ShoppingCartModule(
            IShoppingCartStore shoppingCartStore,
            IProductCatalogClient productCatalog,
            IEventStore eventStore
        ): base("/shoppingCart"){
            Get("/{userid:int}", parameters =>
                {
                    var userId = (int) parameters.userid;
                    return shoppingCartStore.Get(userId);
                }
            );

            Post("/{userid:int}/items",
                async (parameters, _) =>
                {
                    var productCatalogIds = this.Bind<int[]>();
                    var userId = (int) parameters.userid;

                    var shoppingCart = shoppingCartStore.Get(userid);
                    var shoppingCartItems = await
                        productCatalog
                            .GetShoppingCartItems(productCatalogIds) 
                            .ConfigureAwait(false);

                    shoppingCart.AddItems(shoppingCartItems, eventStore);
                    shoppingCartStore.Save(shoppingCart);

                    return shoppingCart;
                }
            );

            Delete("/{userid:int}/items",
                parameters =>
                {
                    var productCatalogIds = this.Bind<int[]>();
                    var userId = (int) parameters.userid;

                    var shoppingCart = shoppingCartStore.Get(userid);
                    shoppingCart.RemoveItems(productCatalogIds, eventStore); 
                    shoppingCartStore.Save(shoppingCart);

                    return shoppingCart;
                }
            );
        }
    }
}