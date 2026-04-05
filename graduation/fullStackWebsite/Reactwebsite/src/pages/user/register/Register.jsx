import React, { useState } from "react";

export default function Register() {
  const baseUrl = import.meta.env.VITE_API_URL;

  const [formData, setFormData] = useState({
    fullName: "",
    address: "",
    email: "",
    password: "",
    phoneNumber: "",
  });

  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [errors, setErrors] = useState("");

  const handleChange = (e) => {
    setFormData((prev) => ({
      ...prev,
      [e.target.name]: e.target.value,
    }));
  };

const handleSubmit = async (e) => {
  e.preventDefault();
  setErrors([]);
  setMessage("");

  try {
    const response = await fetch(`${baseUrl}/api/auth/Account/register`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(formData),
    });

    const data = await response.json();
    console.log("response data:", data);

    if (!response.ok) {
      setMessage(data.message);
      setErrors(data.errors || []);
      return;
    }

    setMessage("Account created successfully, Please check your email to confirm");
    setErrors([]);

    setFormData({
      fullName: "",
      address: "",
      email: "",
      password: "",
      phoneNumber: "",
    });
  } catch (error) {
    setMessage("Something went wrong");
    setErrors([]);
  }
};

  return (
    <div className="min-h-screen bg-[#f7f4f1] flex items-center justify-center px-6 py-12">
      <div className="w-full max-w-[550px] bg-white shadow-md rounded-2xl p-8">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-semibold text-[#bc4c2a]">Create Account</h1>
          <p className="text-gray-500 mt-2">Register your new account</p>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-5">
          <div>
            <label className="block mb-2 text-sm text-gray-700">Full Name</label>
            <input
              type="text"
              name="fullName"
              value={formData.fullName}
              onChange={handleChange}
              placeholder="Leena Abd"
              className="w-full border border-gray-300 rounded-lg px-4 py-3 outline-none focus:border-[#bc4c2a]"
              required
            />
          </div>

          <div>
            <label className="block mb-2 text-sm text-gray-700">Address</label>
            <input
              type="text"
              name="address"
              value={formData.address}
              onChange={handleChange}
              placeholder="palestine"
              className="w-full border border-gray-300 rounded-lg px-4 py-3 outline-none focus:border-[#bc4c2a]"
              required
            />
          </div>

          <div>
            <label className="block mb-2 text-sm text-gray-700">Email</label>
            <input
              type="email"
              name="email"
              value={formData.email}
              onChange={handleChange}
              placeholder="example@gmail.com"
              className="w-full border border-gray-300 rounded-lg px-4 py-3 outline-none focus:border-[#bc4c2a]"
              required
            />
          </div>

          <div>
            <label className="block mb-2 text-sm text-gray-700">Phone Number</label>
            <input
              type="text"
              name="phoneNumber"
              value={formData.phoneNumber}
              onChange={handleChange}
              placeholder="0799999999"
              className="w-full border border-gray-300 rounded-lg px-4 py-3 outline-none focus:border-[#bc4c2a]"
              required
            />
          </div>

          <div>
            <label className="block mb-2 text-sm text-gray-700">Password</label>
            <input
              type="password"
              name="password"
              value={formData.password}
              onChange={handleChange}
              placeholder="********"
              className="w-full border border-gray-300 rounded-lg px-4 py-3 outline-none focus:border-[#bc4c2a]"
              required
            />
          </div>
{errors.length > 0 ? (
  <div className="mb-4">
    {errors.map((err, index) => (
      <p key={index} className="text-red-600 text-sm">
        {err}
      </p>
    ))}
  </div>
) : message ? (
  <p className="text-green-600 text-sm mb-2">{message}</p>
) : null}


          <button
            type="submit"
            disabled={loading}
            className="w-full bg-[#bc4c2a] text-white py-3 rounded-lg hover:opacity-90 transition disabled:opacity-60"
          >
            {loading ? "Registering..." : "Register"}
          </button>
        </form>
      </div>
    </div>
  );
}