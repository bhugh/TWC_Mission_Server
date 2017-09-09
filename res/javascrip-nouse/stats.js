// JavaScript Document
function initSort() {
						$("table").fixheadertable({ 
							caption : 'Stats', 
							colratio : [250, 250, 250,75,75,
                          75,75,75,75,75,
                          75,75,75,75,75,
                          75,75,75,75,75,
                          75,75,75,75,75
                          ], 
							height : 524, 
							//width : 250*3 + 75 * 22 +100,
              width: 800,
              //width : 2452,
							zebra : true,
							sortable : true,
							sortedColId : 1, 
							sortType : ['string', 'string', 'string', 'string', 'integer', 
                          'string', 'integer', 'float', 'float', 'string', 
                          'integer', 'float','integer', 'integer', 'string', 
                          'string', 'integer', 'integer','string', 'integer', 
                          'integer', 'string', 'string', 'integer', 'string'],
							dateFormat : 'm/d/Y',
							pager : false,
							rowsPerPage	 : 50,
							resizeCol : true
						});
}