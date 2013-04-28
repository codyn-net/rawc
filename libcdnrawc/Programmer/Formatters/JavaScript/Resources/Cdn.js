Cdn = Cdn || {};

if (!Cdn.EventValue)
{
	Cdn.EventValue = function(data, i) {
		this.data = data;
		this.i = i;
	};

	Cdn.EventValue.prototype.previous = function() {
		return this.data[i];
	};

	Cdn.EventValue.prototype.current = function() {
		return this.data[i + 1];
	};

	Cdn.EventValue.prototype.distance = function()
	{
		return this.data[i + 2];
	};
}

if (!Cdn.Networks)
{
	Cdn.Networks = {};
}