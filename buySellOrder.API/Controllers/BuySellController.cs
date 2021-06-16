using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace buySellOrder.API.Controllers
{
    public class InputData
    {
        public string Amount { get; set; }
        public string Type { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class BuySellController : ControllerBase
    {
        // POST: api/BuySell
        [HttpPost]
        public ActionResult<List<OrderBookItem>> Post([FromBody] InputData inputData)
        {
            return buySellOrder.Program.bestExecution(inputData.Amount, inputData.Type);
        }
    }
}
